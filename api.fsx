#r "packages/Suave/lib/net40/Suave.dll"
#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "parsing.fsx"

open Suave
open Suave.Web
open Suave.Successful
open Suave.Operators
open Suave.Filters
open Suave.RequestErrors
open FSharp.Data
open Parsing

let sendToServer json=
    let result = FSharp.Data.Http.RequestString("http://scribble.doc.ic.ac.uk/graph.json",
                                                    headers = [ FSharp.Data.HttpRequestHeaders.ContentType HttpContentTypes.Json ],
                                                    body = TextRequest json )
    result

let browse =
    request (fun r ->
        match r.queryParam "json" with
        | Choice1Of2 json -> 
                             let (response:string) = sendToServer json
                             let result = Parsing.getFSMJson response
                             match result with
                                |None -> OK "SCRIBBLE_ERROR : either the code, the protocol name given or the local role is incorrect"
                                |Some jsonResult -> OK jsonResult
        | Choice2Of2 msg -> BAD_REQUEST msg)


let app =
    GET >=> path "/graph.json" >=> browse  