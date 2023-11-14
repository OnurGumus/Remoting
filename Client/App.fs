module Client

open Lit.Elmish
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open System
open Browser
open Elmish
open Lit
open Shared.Domain
open Shared.API
open Elmish.Bridge
let private hmr = HMR.createToken ()

type Model = { Value : NonEmptyString; Counters : Counter.Model list; CounterNumber : int ; ServerConnected : bool }

type Message = 
    | ReverseClicked of string
    | ReverseResult of NonEmptyString
    | CounterMsg of Counter.Message * int
    | AddCounter
    | Remote of ServerToClient.Msg

module Server =
    open Fable.Remoting.Client
    let api: IReverseAPI =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder Client.builder
        |> Remoting.buildProxy<IReverseAPI>

let runCmd input =
    Cmd.OfAsync.perform
        Server.api.Reverse
        input
        ReverseResult

let init () = { Value = NonEmptyString "test"; Counters = []; CounterNumber = 0; ServerConnected = false} , Cmd.none

let update (bridgSend: ClientToServer.Msg ->unit) msg model = 
    match msg with 
    | ReverseClicked string -> { model with Value = NonEmptyString string}, (runCmd  (NonEmptyString string))
    | ReverseResult result -> { model with Value = result }, Cmd.none
    | AddCounter -> { model with CounterNumber = model.CounterNumber + 1 }, Cmd.ofEffect (fun _ -> bridgSend (failwith "what message tosend)"))
    | Remote (ServerToClient.Msg.CounterAdded i) -> 
        let counter, counterCmd = failwith "init sub counter"
        { model with Counters = model.Counters @ [counter] }, Cmd.map (fun msg -> CounterMsg (msg, i)) counterCmd
    | Remote ServerToClient.Msg.ServerConnected  -> 
       { model with ServerConnected = failwith "what here"}, Cmd.none
    | CounterMsg (msg, i) ->
        let bridgeSend = (fun m -> ClientToServer.CounterMessage(m,i)) >> failwith "what here"
        let counter, counterCmd = Counter.update bridgeSend msg model.Counters.[i-1]
        let counters = model.Counters |> List.mapi (fun j c -> if (j + 1) = i then counter else c)
        { model with Counters = counters }, Cmd.map (fun msg -> CounterMsg (msg, i)) counterCmd
     | msg ->
        invalidOp
        <| sprintf "not supported case %A %A" msg model

let mapClientMsg msg =
    match msg with
    | ServerToClient.CounterMessage (m,i) ->
        CounterMsg (Counter.map m,i)
    | _ -> Remote msg


[<HookComponent>]
let view (model:Model) dispatch = 
    Hook.useHmr hmr
    let input = Hook.useRef<HTMLInputElement>()
    let (NonEmptyString text) =  model.Value
    let button = 
        match model.ServerConnected with
        | true -> html $"<button @click={Ev(fun _ -> dispatch AddCounter )} >Add Counter </button>"
        | false -> Lit.nothing
        
    let counters = 
        model.Counters |> List.mapi (fun i counter -> Counter.view counter (fun msg -> dispatch (CounterMsg (msg,i + 1))))

    html $"""
    <input .value={text} type='text' {Lit.refValue input } />
    <button @click={Ev(fun _ -> dispatch (ReverseClicked input.Value.Value.value ))} >Reverse </button>
    {button}
    {counters}
    """
  
let bc = Bridge.endpoint clientEndpoint |>  Bridge.withUrlMode UrlMode.Raw |>  Bridge.withMapping mapClientMsg 

Program.mkProgram init (update Bridge.Send) view
|> Program.withLit "main"
|> Program.withBridgeConfig bc
|> Program.withConsoleTrace
|> Program.run