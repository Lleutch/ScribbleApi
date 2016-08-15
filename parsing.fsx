#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
open System
open System.Text.RegularExpressions

type ArrayJson = FSharp.Data.JsonProvider<"""[{"result":" 1 -> 3 [ label=Me!World:hello() ]"}]""">
type DiGraph = FSharp.Data.JsonProvider<""" {"result":"value"} """>

let internal replaceRegex (pattern:string) (input:string) (replacement:string) =
    Regex.Replace(input,pattern,replacement)

let internal replaceString (old:string) (input:string) (newValue:string) =
    input.Replace(old,newValue)

let getArrayJson (json:string) =
    let s = DiGraph.Parse(json)

    let s0 = s.Result
    match Regex.IsMatch(s0,"java\\.lang\\.NullPointerException") with
        |true ->  None
        |false ->   let oldValue = "digraph G {\ncompound = true;"
                    let result = s0  
                                    |> replaceString oldValue <|"[" 
                                    |> replaceString "\n" <| "" 
                                    |> replaceRegex "\"\d+\";" <| ""
                                    |> replaceRegex "\"\s\];" <|" ]\"},"
                                    |> replaceRegex "\"(\d+)\"\s->\s\"(\d+)\"" <| "{\"result\":\" $1 -> $2"
                                    |> replaceRegex "\]\"\},\}" <| "]\"}]"
                                    |> replaceRegex "label=\"" <| "label="
                    Some result

let getFSMJson (json:string) =
    
    let arrayJson = getArrayJson json
    match arrayJson with
        |None -> None
        |Some jsonvalue -> 
                let arrayJsonString = ArrayJson.Parse(jsonvalue)
                let json = new System.Text.StringBuilder()
                json.Append("[") |> ignore
                let size = arrayJsonString.Length
                for i in 0..(size-1) do 
                    let elem = arrayJsonString.[i]
                    // Yeah I know, this regex is burning your eyes :)
                    let regex = "\s(\d+)\s->\s(\d+)\s\[\slabel=(\w+)\[(\w+)\](\w+):(\w+)\(([\s*class\s*(\w+\\.\w+)\s*\\,*]*)\)\s*\]" 
                    let typeOfMessage = if Regex.IsMatch(elem.Result,"\w+(\!)\w+:") then 
                                            Regex.Replace(elem.Result,"(\w+)\!(\w+):","$1[send]$2:")
                                        elif Regex.IsMatch(elem.Result,"\w+(\?)\w+:") then 
                                            Regex.Replace(elem.Result,"(\w+)\?(\w+):","$1[receive]$2:")
                                        else
                                            failwith "There is a wrong type of method: expected '!' or '?' but received something different"
                    let mutable replace = "{\"currentState\": $1 , \"localRole\": \"$3\" , \"partner\": \"$5\" , \"label\": \"$6\" , \"payload\": [$7] , \"type\": \"$4\" , \"nextState\": $2 }"       
                    if not(i=(size-1)) then
                        replace <- sprintf "%s%s" replace ","
                    let replaced = Regex.Replace(typeOfMessage,regex,replace)
                    let regex2 = "\s*class\s*(\w+\\.\w+)\s*\\,"
                    let replace2 = "\"$1\"," 
                    let really = Regex.Replace(replaced,regex2,replace2)
                    let regex3 = "\s*class\s*(\w+\\.\w+)\s*\]"
                    let replace3 = "\"$1\"]" 
                    let reallyReally = Regex.Replace(really,regex3,replace3)
                    json.Append(reallyReally) |> ignore

                json.Append("]") |> ignore
                Some (json.ToString())