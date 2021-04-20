
module Handlers
open System
open System.Text
open System.IO
open Microsoft.Net.Http.Server
open Microsoft.Extensions.Primitives
open System.Net.Http

open FSharp.Data

let getContent root (relativePath : string) =
    try
        let ext = FileInfo(root + relativePath).Extension
        let content = File.ReadAllBytes(root + relativePath)
        Ok(int64 content.Length, content, ext)
    with e -> 
        printfn "error: %s" e.Message
        Error ()
    
let error =
   Encoding.ASCII.GetBytes("<!DOCTYPE html>
    <html lang=\"en\">
      <head>
        <meta charset=\"utf-8\">
        <title>Hello!</title>
      </head>
      <body>
        <h1>Oops!</h1>
        <p>Sorry, I don't know what you're asking for.</p>
      </body>
    </html>")

let requestHandler (root : string) (request : Request, responce : Response) =
    printfn "url: %s" (root + request.Path)
    let fetch =
        match request.Path with
        | "/" ->
            getContent root "/index.html"
        | url ->
            // don't expect queury at the moment
            getContent root url
    
    match fetch with
    | Error _ ->
        responce.StatusCode <- 404
        responce.ContentLength <- int64 error.Length
        responce.Body.Write (ReadOnlySpan(error))
    | Ok(sz, content, ext) ->
        let tp = 
            // need to encode more specific text, audio, applications and such
            match ext with
            | ".apng"
            | ".avif"
            | ".gif"
            | ".jpg" 
            | ".jpeg" 
            | ".jfif"
            | ".pjpeg"
            | ".pjp"
            | ".png"
            | ".svg"
            | ".webp" -> "image/" + ext.[1..]

            | _ -> "text/" + ext.[1..]

        responce.StatusCode <- 200
        responce.ContentLength <- sz
        responce.ContentType <- tp
        responce.Body.Write (ReadOnlySpan(content))

// base adress must be set for the httpclient to work
let proxiHandler (client : HttpClient) (req : Request, res : Response) =
    let path = req.Path
    let res' = client.GetAsync(path).Result
    
   
    
    let cntnt = res'.Content.ReadAsByteArrayAsync().Result
    res.StatusCode <- int res'.StatusCode
    for KeyValue(key, values) in res'.Headers do
        // convert old form to new form
        let values' = 
            seq {
                let i = values.GetEnumerator()
                in while i.MoveNext() do
                    yield i.Current
            }
            |> Seq.toArray
        
        res.Headers.Add (key, StringValues(values'))
        
    res.Body.Write(ReadOnlySpan(cntnt))



let filter (tags : string list) att (pattern : string) (webpage : string) =
    use grønt = File.CreateText("C:/Users/KAM/OneDrive/Skrivebord/Grønt/vare.txt")
    
    let page = HtmlDocument.Load webpage

    // get all node with one of the tags
    page.Descendants tags
    |> Seq.filter (fun x ->
        // filter all out with attribute = att
        x.TryGetAttribute(att)
        // where the attribute contains some of the pattern
        |> Option.map (fun att -> att.Value().Contains(pattern))
        |> Option.contains true
        
    )

(*
let t =
    filter ["div"] "data-testid" "product_card_" "https://hjem.foetex.dk/kategori/frugt-og-groent"
    |> Seq.map 
        (fun node -> 
            node.Descendants ["span"]
            |> Seq.map (fun node -> node.InnerText())
            |> Seq.filter ((<>) "")
    )
    |> Seq.tail
    |> Seq.head


*)