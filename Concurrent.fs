module Concurrent

open System.Threading
open System

#nowarn "670"

open Channel

type 'T Work = Off | Job of ('T -> unit) * 'T

type internal 'T Worker =
    val id: int
    val mutable thread: Thread Option


    new(id, channel) = 
        let thread = 
            Thread(ThreadStart( fun _ ->
            let mutable clockedIn = true
            let reader = channel : 'T Work ChannelReader
            while clockedIn do
                match reader.Read() with
                | None -> ()
                | Some job ->
                    match job with
                    | Off -> 
                        printfn "Worker %d says `Finally off!!´" id
                        clockedIn <- false
                    | Job (work, args: 'T) -> 
                        work args
            )   
        )

        thread.Start()

        { id = id; thread = Some thread }




type 'T WorkForce =
    val internal workers: 'T Worker[]
    val channel: ('T Work) ChannelWriter 

    interface IDisposable with
        member W.Dispose() =
            Array.iter (fun _ -> 
                let mutable cur = W.channel.Write Off
                let mutable writing = true
                // send a shutdown message to the workforce
                // since we iterate over the workforce it will always send
                // the same number of messages as the number of workers
                while writing do
                    // invariance here is that the loop only runs when not returning None
                    // i.e. the is always a value
                    cur <- W.channel.Write cur.Value
                    match cur with
                    | None -> 
                        writing <- false
                    | _ -> 
                        ()
                ) W.workers

            Array.iter (fun (worker : 'T Worker) -> 
                match worker.thread with
                | None -> ()
                | Some thread -> thread.Join()
                ) W.workers


    new(size) =
        let (writer: 'T Work ChannelWriter, reader: 'T Work ChannelReader) = Channel<'T Work>.Open()
        
        {
            workers = [| for i in 0 .. size - 1 do (new Worker<'T>(i, reader))|]
            channel = writer
        }

    member W.Do work args =
        let mutable work' = Some(Job(work, args))
        while work'.IsSome do
            work' <- W.channel.Write (work'.Value)
            