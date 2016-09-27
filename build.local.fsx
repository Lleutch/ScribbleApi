(*#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/FAKE/tools/FakeLib.dll"
#load "api.fsx"
open Api
open Fake
open System
open System.IO
open Suave
open Suave.Http
open Suave.Web

let serverConfig =
    let port = Sockets.Port.Parse <| getBuildParamOrDefault "port" "8083"
    { defaultConfig with
        homeFolder = Some __SOURCE_DIRECTORY__
        logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Warn
        bindings = [ HttpBinding.mk HTTP System.Net.IPAddress.Loopback port ] }

Target "run" (fun _ ->
    startWebServer serverConfig app
)

RunTargetOrDefault "run"*)

#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/FAKE/tools/FakeLib.dll"
#load "api.fsx"

open Api
open System
open System.IO
open Fake

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

// Step 2. Use the packages

#r "packages/Suave/lib/net40/Suave.dll"

open Suave // always open suave
open Suave.Successful // for OK-result
open Suave.Web // for config
open System.Net

let port = Sockets.Port.Parse <| getBuildParamOrDefault "port" "8083"

let serverConfig = 
    {defaultConfig with
        bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ]
    } 
startWebServer serverConfig app 