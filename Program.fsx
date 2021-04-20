// Learn more about F# at http://fsharp.org

open System
open System.Text
open System.IO
open Microsoft.Net.Http.Server
open System.Net.Http
open Server
open Handlers

// This is an implementation of a simple proxi server
// it simply echo the request to the real server, and echo the responce back to the client.
// there is no caching of data, but that could easily be implemented.
[<EntryPoint>]
let main argv =

    match argv with
    | [| "proxi"; location; root |] -> 
    // when released this needs to be set as argument
        use client = 
            let c = new HttpClient()
            c.BaseAddress <- Uri root
            c

        let settings =
            let s = WebListenerSettings()
            s.UrlPrefixes.Add(location)
            s

        using 
            (new AsyncWebServer(settings))
            (fun server ->
                printfn "the server is running"
                printfn "root %s" root
                server.Run (proxiHandler client)
        )
        

    |  [| "main"; location; root |] ->
        let settings =
            let s = WebListenerSettings()
            s.UrlPrefixes.Add(location)
            s

        using 
            (new AsyncWebServer(settings))
            (fun server ->
                printfn "the server is running"
                printfn "root %s" root
                server.Run (requestHandler root)
        )
        
    | _ ->
        let location, root = "http://localhost:8080", "https://netto.dk/"
        use client = 
            let c = new HttpClient()
            c.BaseAddress <- Uri root
            c

        let settings =
            let s = WebListenerSettings()
            s.UrlPrefixes.Add(location)
            s

        using 
            (new AsyncWebServer(settings))
            (fun server ->
                printfn "the server is running"
                printfn "root %s" root
                server.Run (proxiHandler client)
        )

    0