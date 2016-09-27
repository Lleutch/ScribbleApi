#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
open System
open System.Text.RegularExpressions

type ArrayJson = FSharp.Data.JsonProvider<"""[{"result":" 1 -> 3 [ label=Me!World:hello() ]"}]""">
type DiGraph = FSharp.Data.JsonProvider<""" {"result":"value"} """>
type ScribbleProtocole = FSharp.Data.JsonProvider<""" [ { "currentState":0 , "localRole":"StringLocalRole" , "partner":"StringPartner" , "label":"StringLabel" , "payload":["StringTypes"] , "type":"EventType" , "nextState":0  } ] """>
                    

//let json = ScribbleAPI.Root() //.Root(code = replace2 , proto = protocol ,role = localRole )

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

let modifyAllSame (c:ScribbleProtocole.Root []) (couple:int*int)  =
    let mutable newArray = [||]
    for elem in c do
        match elem.NextState = (couple |> fst) with
            | false -> newArray <- Array.append newArray [|elem|]  
            | true -> let newElem = ScribbleProtocole.Root(elem.CurrentState,elem.LocalRole,elem.Partner,elem.Label,elem.Payload,elem.Type,(couple |> snd))
                      newArray <- Array.append newArray [|newElem|]
    newArray

let isIn listIndex index = listIndex |> List.exists(fun x -> x = index)

let isCurrentChoice (fsm:ScribbleProtocole.Root []) (index:int) =
    let current = fsm.[index].CurrentState
    let mutable size = 0 
    for elem in fsm do
        if elem.CurrentState = current then
            size <- size + 1
    (size>1)

let modifyAllChoice (fsm:ScribbleProtocole.Root []) =
    let mutable newArray = [||] 
    for i in 0..(fsm.Length-1) do
        let elem = fsm.[i]
        if elem.Type = "receive" && (isCurrentChoice fsm i) then
            let newElem = ScribbleProtocole.Root(elem.CurrentState,elem.LocalRole,elem.Partner,elem.Label,elem.Payload,"choice",elem.NextState)
            newArray <- Array.append newArray [|newElem|]            
        else
        newArray <- Array.append newArray [|elem|]
    newArray


let getFSMJson (json:string) =
    let mutable (listOfCouples:(int*int) list) = []
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
                    let regex = "\s(\d+)\s->\s(\d+)\s\[\slabel=(\w+)\[(\w+)\](\w+):(\w+)\(([\s*class\s*(\w+\\.\w+\[*\]*)\s*\\,*]*)\)\s*\]" 
                    let typeOfMessage = if Regex.IsMatch(elem.Result,"\w+(\!)\w+:") then 
                                            Regex.Replace(elem.Result,"(\w+)\!(\w+):","$1[send]$2:")
                                        elif Regex.IsMatch(elem.Result,"\w+(\?)\w+:") then 
                                            Regex.Replace(elem.Result,"(\w+)\?(\w+):","$1[receive]$2:")
                                        elif Regex.IsMatch(elem.Result,"(\d+)\s*->\s*(\d+)\s*[\s*label=__tau()\s*]\s*") then // this is for epsilon transition
                                            let matchReg = Regex.Match(elem.Result,"(\d+)\s*->\s*(\d+)\s*[\s*label=__tau()\s*]\s*")
                                            let groups = matchReg.Groups
                                            let current = System.Int32.Parse(groups.[1].Value)
                                            let next = System.Int32.Parse(groups.[2].Value)
                                            listOfCouples <- (current,next)::listOfCouples
                                            ""
                                        else
                                            failwith (sprintf "There is a wrong type of method: expected '!' or '?' but received something different : %s " elem.Result ) 
                    let mutable replace = "{\"currentState\": $1 , \"localRole\": \"$3\" , \"partner\": \"$5\" , \"label\": \"$6\" , \"payload\": [$7] , \"type\": \"$4\" , \"nextState\": $2 }"       
                    if not(i=(size-1)) then
                        replace <- sprintf "%s%s" replace ","
                    let replaced = Regex.Replace(typeOfMessage,regex,replace)
                    let regex2 = "\s*class\s*(\w+\\.\w+)\s*\\,"
                    let replace2 = "\"$1\"," 
                    let tmp = Regex.Replace(replaced,regex2,replace2)
                    let regex3 = "\s*class\s*(\w+\\.\w+\[*\]*)\s*\]"
                    let replace3 = "\"$1\"]" 
                    let real = Regex.Replace(tmp,regex3,replace3)
                    json.Append(real) |> ignore

                json.Append("]") |> ignore
                let tmpJson = Regex.Replace(json.ToString(),"\},\]","}]")
                let newJson = new System.Text.StringBuilder(tmpJson)
                let mutable jsonTyped = ScribbleProtocole.Parse(newJson.ToString()) 
                for couple in listOfCouples do 
                    jsonTyped <- modifyAllSame jsonTyped couple
                let resultTyped = modifyAllChoice jsonTyped
                let result = new System.Text.StringBuilder()
                result.Append("[") |> ignore
                let size = resultTyped.Length
                for i in 0..(size-1) do 
                    let elem = resultTyped.[i] 
                    let length = elem.Payload.Length
                    let payload = if (length = 0) then // ou si c'est 1
                                    ""
                                  elif (length = 1) then 
                                    sprintf "\"%s\"" elem.Payload.[0]
                                  else 
                                    let first = sprintf "\"%s\"" elem.Payload.[0]
                                    (elem.Payload.[1..(length-1)] |> Array.fold(fun acc x -> sprintf "%s,\"%s\"" acc x) first )
                    let mutable tmpValue = sprintf "{\"currentState\": %d , \"localRole\": \"%s\" , \"partner\": \"%s\" , \"label\": \"%s\" , \"payload\": [%s] , \"type\": \"%s\" , \"nextState\": %d }"
                                                elem.CurrentState elem.LocalRole elem.Partner elem.Label payload elem.Type elem.NextState
                    if not(i=(size-1)) then
                        tmpValue <- sprintf "%s%s" tmpValue ","
                    result.Append(tmpValue) |> ignore

                
                result.Append("]") |> ignore
                Some (result.ToString())