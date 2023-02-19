module Shared.API

open Shared.Domain

type IReverseAPI = {
     Reverse : Reverse
}

let endpoint = "/socket"

module Client =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName = sprintf "http://localhost:5000/api/%s/%s" typeName methodName
module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName = sprintf "/api/%s/%s" typeName methodName