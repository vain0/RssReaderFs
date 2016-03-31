﻿namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type Ctrl (rc: RssClient) =
  let view =
    new View(rc)

  member this.Update(srcOpt) =
    async {
      let src =
        defaultArg srcOpt (rc.Reader |> RssReader.allFeedSource)
      return! rc.UpdateAsync src
    }

  member this.CheckNewItemsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! newItems = this.Update(None)
        do
          if newItems |> Array.isEmpty |> not then
            view.PrintCount(newItems)
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.UpdateAndShowCount(srcOpt) =
    async {
      let! items = this.Update(srcOpt)
      do view.PrintCount(items)
    }

  member this.UpdateAndShowDetails(srcOpt) =
    async {
      let! items = this.Update(srcOpt)
      do view.PrintItems(items)
    }

  member this.TryFindSource(srcName) =
    rc.Reader
    |> RssReader.tryFindSource srcName
    |> tap (fun opt ->
      if opt |> Option.isNone then
        eprintfn "Unknown source: %s" srcName
      )

  member this.Interactive() =
    let rec loop () = async {
      let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
      match line with
      | null | "" ->
          return! loop ()
      | "quit" | "halt" | "exit" ->
          ()
      | line ->
          let command =
            line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
          match command with
          | "update" :: _ ->
              do! this.UpdateAndShowCount(None)

          | "show" :: srcName :: _ ->
            match this.TryFindSource(srcName) with
            | None -> ()
            | Some src ->
                do! this.UpdateAndShowDetails(Some src)

          | "show" :: _ ->
            do! this.UpdateAndShowDetails(None)

          | "feeds" :: _ ->
              let body () =
                rc.Reader
                |> RssReader.allFeeds
                |> Array.iter (fun src ->
                    printfn "%s <%s>"
                      (src.Name) (src.Url |> Url.toString)
                  )
              in lockConsole body

          | "feed" :: name :: url :: _ ->
              let feed = RssFeed.create name url
              let body () =
                rc.AddSource(feed |> RssSource.ofFeed)
              in lockConsole body

          | "remove" :: name :: _ ->
              let body () =
                rc.Reader
                |> RssReader.tryFindSource name
                |> Option.iter (fun src ->
                    rc.RemoveSource(name)
                    printfn "'%s' has been removed."
                      (src |> RssSource.name)
                    )
              in lockConsole body

          | "sources" :: _ ->
              let body () =
                rc.Reader
                |> RssReader.sourceMap
                |> Map.toList
                |> List.iter (fun (_, src) ->
                    printfn "%s" (src |> RssSource.toSExpr)
                    )
              in lockConsole body

          | "tag" :: tagName :: srcName :: _ ->
              let body () =
                match rc.Reader |> RssReader.tryFindSource srcName with
                | Some src -> rc.AddTag(tagName, src)
                | None -> printfn "Unknown source name: %s" srcName
              in lockConsole body

          | "detag" :: tagName :: srcName :: _ ->
              let body () =
                match rc.Reader |> RssReader.tryFindSource srcName with
                | Some src -> rc.RemoveTag(tagName, src)
                | None -> printfn "Unknown source name: %s" srcName
              in lockConsole body

          | "tags" :: _ ->
              let body () =
                rc.Reader
                |> RssReader.tagMap 
                |> Map.iter (fun tagName srcs ->
                    printfn "%s %s"
                      tagName
                      (String.Join(" ", srcs |> Set.map (RssSource.toSExpr)))
                    )
              in lockConsole body

          | _ -> ()
          return! loop ()
      }
    in loop ()
