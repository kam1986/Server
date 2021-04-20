/// This is of no intent a way to optain a better performance than Task and async.
/// it is only a way to limit the number of threads allowed to do stuff.
module Channel
open System.Threading

// This is the abstraction connection for the linking for two or more concurrent entities
// both read and write are atomic operations so multiple 
type internal 'T Link =
    val mutable buf : 'T option

    internal new() = { buf = None }

    // return None when no data is accessable
    // make it easy to use, and none blocking with little overhead
    member internal C.Read() = Interlocked.Exchange(&C.buf, None)

    // Return current data, to make the user deside wether or not
    // to make the channel blocking or non blocking.
    member internal C.Write value = Interlocked.Exchange(&C.buf, Some value)
        

/// Represent an atomic reader of a channel
///
/// The Read method return the last written data to the channel. There is no garuanty about data being missed.
/// but this can be implemented by the user.
/// to test if there is new data test the return value of Read, if it return None no new data has been writen since last read.
/// else it will return 'Some value'
type 'T ChannelReader =
    val internal link : 'T Link
    
    internal new link = { link = link }
    
    member C.Read() = 
        let ret = C.link.Read()
        ret

    member C.HasNewData() =
        match C.link.buf with
        | None  -> false
        | _     -> true

/// Represent an atomic writer of a channel
///
/// The Write method return the last written data if it has not been read.
/// this way it the user can always rewrite it into the channel if needed.
/// ALWAYS test the return of the write method, if it return None, there has been no overwritten data
/// and we can safely progress, else it is up to the user to handle this.
type 'T ChannelWriter =
    val internal link : 'T Link
    
    internal new link = { link = link }
    
    member C.Write value =
        let ret = C.link.Write value
        ret

    // This has no need for atomicy since it do not change the data
    member C.IsRead =
        match C.link.buf with
        | None  -> true
        | _     -> false



/// This is a none blocking, none locking, and none buffering channel type
/// It is up to the user to handle overwriting of old data before it is read.
/// data is never destroyed unless explicitly doing so by the user.
/// Old data are returned to the writer by the Write method and can be handled by 
/// the implementator.
/// if order of the data is of concern use the IsRead in the ChannelWriter
[<Struct>]
type 'T Channel = 
    // this is an empty struct used as a wrapper to create the illusion of a channel.
    static member Open() = 
        let link = Link()
        ChannelWriter<'T> link, ChannelReader<'T> link

    
    
let testChannel() =
    let case = [| for i in 1 .. 10000 -> i |]
    let ret =  [| for _ in 1 .. 10000 -> 0 |]
    assert(ret <> case) // make sure that the result are not imitated
    
    let writer, reader = Channel.Open()

    let w = 
        Thread(ThreadStart(fun _ ->
            let mutable cur = None
            for i in 1 .. 10000 do
                cur <- writer.Write(case.[i-1])
                while cur <> None do // make sure that every entry are kopied
                    cur <- writer.Write(cur.Value)
        ))

    let r =
        Thread(ThreadStart(fun _ ->
            let mutable cur = None
            for i in 1 .. 10000 do
                cur <- reader.Read()
                while cur = None do
                    cur <- reader.Read()
                ret.[i-1] <- cur.Value
                cur <- None
        ))

    w.Start()
    r.Start()
    w.Join()
    r.Join()
    let ret = Array.sort ret
    assert(case = ret)
    printfn "Success"

