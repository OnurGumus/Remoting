module Shared.Domain
open Newtonsoft.Json

#if !FABLE
    [<JsonObject(MemberSerialization = MemberSerialization.Fields)>]
#endif
type NonEmptyString = NonEmptyString of string

type Reverse = NonEmptyString -> NonEmptyString Async

