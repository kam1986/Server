module Server


open System
open Microsoft.Net.Http.Server
open Concurrent
    
/// A http server with optional declaring a number of worker threads to handle
/// the workload. If workers < 1 then it will set it to 1.
/// course a runtime error on shutdown
type WebServer =
    val conn : WebListener
    val workers: WorkForce<Request * Response>
    val mutable isRunning : bool

    // making the server easier to use.
    // i.e. does not need to close the socket, and handle threads
    interface IDisposable with
        member Server.Dispose() =
            // shout down the connection to the world
            Server.conn.Dispose()
            // send signal to the workforce that they should stop
            // and wait undtil they have done that.
            (Server.workers :> IDisposable).Dispose()

    new(settings, ?workers) = 
        let conn = new WebListener(settings)
        let workers =
            match workers with
            | Some w when w > 0 -> w
            | None | Some _ -> 1

        
        {
            conn = conn
            workers = new WorkForce<_>(workers)
            isRunning = false
        }

    
    member Server.Dispose() = (Server :> IDisposable).Dispose()

    /// Run a handler function on the Http server. 
    member Server.Run handler =
        Server.conn.Start()
        Server.isRunning <- true
        
        async {
            while Server.isRunning do
                let input = Console.ReadLine().ToLower() 
                match input with
                | "exit" -> 
                    printfn "stop"
                    Server.isRunning <- false
                | input -> 
                    printfn "The input: %s is not a valid command" input

        }
        |> Async.Start


        while Server.isRunning do
            
            // get a request
            let requestcontext = Server.conn.AcceptAsync().Result
            
            // make some worker handle the request
            Server.workers.Do 
                (fun r -> 
                    handler r
                    requestcontext.Dispose()
                )
                (requestcontext.Request, requestcontext.Response)
            
        Server.Dispose()
           
        
type AsyncWebServer =
    val conn : WebListener 
    val mutable isRunning : bool

    interface IDisposable with
        member Server.Dispose() =
            Server.conn.Dispose()
    
    
    new(settings) = 
        { 
            conn = new WebListener(settings)
            isRunning = false
        }

    member Server.Dispose() = (Server :> IDisposable).Dispose()

    member Server.Run handler =
        Server.conn.Start()
        Server.isRunning <- true
        
        async {
            while Server.isRunning do
                match Console.ReadLine().ToLower() with
                | "exit" -> 
                    printfn "stop"
                    Server.isRunning <- false
                | input -> 
                    printfn "The input: %s is not a valid command" input

        }
        |> Async.Start

        while Server.isRunning do

            let requestcontext = Server.conn.AcceptAsync().Result
            async {
                handler (requestcontext.Request, requestcontext.Response)
                requestcontext.Dispose()
            }
            |> Async.Start 

        Server.Dispose()