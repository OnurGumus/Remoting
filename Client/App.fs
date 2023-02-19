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

let private hmr = HMR.createToken ()

type Model = NonEmptyString

type Message = 
    | ReverseClicked of string
    | ReverseResult of NonEmptyString


module Server =
    open Shared.API
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

let init () = NonEmptyString "test" , Cmd.none

let update msg model = 
    match msg with 
    | ReverseClicked string -> NonEmptyString string, runCmd  (NonEmptyString string)
    | ReverseResult result -> result, Cmd.none

[<HookComponent>]
let view (model:Model) dispatch = 
    Hook.useHmr hmr
    let input = Hook.useRef<HTMLInputElement>()
    let (NonEmptyString text) =  model
    html $"""
    <input .value={text} type='text' {Lit.refValue input } />
    <button @click={Ev(fun _ -> dispatch (ReverseClicked input.Value.Value.value ))} >Reverse </button>
    """



Program.mkProgram init update view
|> Program.withLit "main"
|> Program.run