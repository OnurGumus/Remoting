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