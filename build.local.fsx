#r "packages/Suave/lib/net40/Suave.dll"
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

RunTargetOrDefault "run"