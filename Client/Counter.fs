module Counter

open Elmish
open Lit
open Shared.API

type Message =
    | SetValue of int
    | Start
    | Stop
    | Remote of ServerToClient.CounterMsg

type Model = { Counter : int; Started :bool }

let init () = { Counter = 0; Started = false} ,Cmd.none

let update (bridgeSend:ClientToServer.CounterMsg-> unit) msg model =
    match msg with
    | SetValue i -> { model with Counter = i}, Cmd.none
    | Start -> { model with Started = true},  Cmd.ofEffect(fun _ -> bridgeSend ClientToServer.StartCounter)
    | Stop -> { model with Started = false}, Cmd.ofEffect(fun _ -> bridgeSend ClientToServer.StopCounter)
    | Remote (ServerToClient.CounterMsg.CounterValue i) -> model, Cmd.ofMsg (SetValue i)

let map msg =
    match msg with
    | _ -> Remote msg

let view model dispatch = 
    html $"""
    <div >Counter: {model.Counter}</div>
    <button @click={Ev(fun _ -> dispatch Start)}>Start</button>
    <button @click={Ev(fun _ -> dispatch Stop)}>Stop</button>
    """

