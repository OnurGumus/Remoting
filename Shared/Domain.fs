module Shared.Domain
open Newtonsoft.Json

type NonEmptyString = NonEmptyString of string

type Reverse = NonEmptyString -> NonEmptyString Async

