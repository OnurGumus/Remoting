module Server

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Shared.API
open Shared.Domain
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open Shared.API
open Elmish
open Elmish.Bridge



//let hub = ServerHub()
module Counter  =
    open System.Threading
    type Model = { Count: int ; Token: CancellationTokenSource option}
    type Msg = Start | Stop | Tick | Remote of ClientToServer.CounterMsg

    let cmd  (token:CancellationToken)= Cmd.ofEffect(fun dispatch ->
        Async.StartImmediate(
         async {
            while not token.IsCancellationRequested do
                do! Async.Sleep 1000
                dispatch Tick
        }, token ))

            
    let init () = {Count = 0; Token = None;}, Cmd.none
    let update clientDispatch msg model =
        match msg with
        | Remote (ClientToServer.CounterMsg.StartCounter) -> model, Cmd.ofMsg Start
        | Remote (ClientToServer.CounterMsg.StopCounter) -> model, Cmd.ofMsg Stop
        | Start ->
            let tokenSource = new CancellationTokenSource()
            let token = tokenSource.Token
            { model with Token = Some tokenSource  }, cmd token
        | Stop -> 
            match model.Token with
            | Some tokenSource -> tokenSource.Cancel(); tokenSource.Dispose(); { model with Token = None }, Cmd.none
            | None -> model, Cmd.none
        | Tick -> 
            clientDispatch (ServerToClient.CounterValue model.Count)
            { model with Count = model.Count + 1 }, Cmd.none

type ServerMsg =
    | Remote of ClientToServer.Msg
    | CounterMsg of int * Counter.Msg
    
type Model = { Counters : Counter.Model list; CounterNumber : int}

let init (clientDispatch:Dispatch<ServerToClient.Msg>) () =
    clientDispatch ServerToClient.ServerConnected
    { Counters = []; CounterNumber = 0}, Cmd.none

let update (clientDispatch:Dispatch<ServerToClient.Msg>) (msg:ServerMsg) model =
    //hub.SendClientIf (fun x -> x < 3) ServerToClient.ServerConnected
    let clientDispatchC i =  (fun m -> ServerToClient.CounterMessage(m,i)) >> clientDispatch
    match msg with
    | Remote ClientToServer.AddCounter ->
        let counterNumber = model.CounterNumber + 1
        let counter, counterCmd = Counter.init ()
        let counters = model.Counters @ [counter]
        { model with Counters = counters; CounterNumber = counterNumber }, 
            Cmd.batch [ 
                Cmd.map (fun msg -> CounterMsg (counterNumber, msg)) counterCmd; 
                Cmd.ofEffect(fun _ ->  clientDispatch (ServerToClient.CounterAdded counterNumber)) ]
    | CounterMsg (counterNumber, msg) ->
        let counter, counterCmd = Counter.update (clientDispatchC counterNumber) msg (model.Counters[counterNumber - 1])
        let counters = model.Counters |> List.mapi (fun i c -> if i = counterNumber - 1 then counter else c)
        { model with Counters = counters }, Cmd.map (fun msg -> CounterMsg (counterNumber, msg)) counterCmd
    | Remote (ClientToServer.CounterMessage (m, i)) ->
        let msg:Counter.Msg = Counter.Remote m
        model,Cmd.ofMsg (CounterMsg (i, msg))



let server =
  Bridge.mkServer Shared.API.endpoint init update
 // |> Bridge.withServerHub hub
  |> Bridge.withConsoleTrace
  |> Bridge.run Giraffe.server


let reverseAPI: IReverseAPI = {
    Reverse = fun (NonEmptyString s) ->
        let reversed = new String(s.ToCharArray() |> Array.rev)
        async{ return (NonEmptyString reversed) }
}


let reverseHandler: HttpHandler =
    Remoting.createApi ()
    |> Remoting.withErrorHandler (fun ex routeInfo -> Propagate ex.Message)
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue reverseAPI
    |> Remoting.buildHttpHandler

let webApp  =
    choose [
        server

        reverseHandler
    ]

let configureServices (services: IServiceCollection) = 
     services
        .AddCors(fun options -> options.AddDefaultPolicy(fun pol-> pol.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod() |> ignore))
        .AddGiraffe().AddRouting()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    app
        .UseWebSockets() 

     .UseRouting().UseCors()
     
        .UseGiraffe(webApp)|> ignore
   
   
[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")
    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseContentRoot(contentRoot)
                .UseWebRoot(webRoot)
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
            |> ignore
        )
        .Build()
        .Run()
    0