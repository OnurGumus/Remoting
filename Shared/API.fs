module Shared.API

open Shared.Domain

type IReverseAPI = {
     Reverse : Reverse
}


// Messages processed on the server
module ServerToClient =
    type CounterMsg = CounterValue of int

    type Msg =
        | ServerConnected
        | CounterAdded of int
        | CounterMessage of CounterMsg * int

module ClientToServer =
    type CounterMsg =
        | StartCounter
        | StopCounter
    type Msg =
        | AddCounter
        | CounterMessage of CounterMsg * int
        



let endpoint = "/socket"

let clientEndpoint = "ws://localhost:5000/socket"
module Client =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName = sprintf "http://localhost:5000/api/%s/%s" typeName methodName
module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName = sprintf "/api/%s/%s" typeName methodName