﻿namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs.Core

type Ctrl (rr: RssReader, sendResult: CommandResult -> Async<unit>) =
  let mutable unreadItems =
    rr |> RssReader.unreadItems (Source.allSource (rr |> RssReader.ctx))

  member this.TryFindSource(srcName) =
    Source.tryFindByName (rr |> RssReader.ctx) srcName
    |> Trial.failIfNone (srcName |> SourceDoesNotExist)

  member this.UpdateAsync(src) =
    async {
      let! newItems = rr |> RssReader.updateAsync src
      if newItems |> Array.isEmpty |> not then
        unreadItems <- Array.append unreadItems newItems
    }

  member this.CheckNewItemsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        do! this.UpdateAsync(Source.allSource (rr |> RssReader.ctx))
        if unreadItems |> Array.isEmpty |> not then
          do! (unreadItems, Count) |> Async.inject |> Trial.inject |> ArticleSeq |> sendResult
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.UpdateAndShow(srcName, fmt) =
    this.TryFindSource(srcName)
    |> Trial.lift (fun src -> async {
        do! this.UpdateAsync(src)
        return
          (unreadItems, fmt)
          |> tap (fun _ -> if fmt <> Count then unreadItems <- [||])
        })
    |> ArticleSeq

  member private this.ProcCommandImpl(command) =
      match command with
      | "update" :: srcName :: _ ->
          this.UpdateAndShow(srcName, Count)
      | "update" :: _ ->
          this.UpdateAndShow(AllSourceName, Count)
      | "show" :: srcName :: _ ->
          this.UpdateAndShow(srcName, Details)
      | "show" :: _ ->
          this.UpdateAndShow(AllSourceName, Details)
      | "list" :: srcName :: _ ->
          this.UpdateAndShow(srcName, Titles)
      | "list" :: _ ->
          this.UpdateAndShow(AllSourceName, Titles)

      | "feeds" :: _ ->
          Source.allFeeds (rr |> RssReader.ctx)
          |> Seq.map (Source.ofFeed)
          |> SourceSeq

      | "feed" :: name :: url :: _ ->
          let result    = rr |> RssReader.addFeed name url
          in result |> Result

      | "twitter-user" :: name :: _ ->
          let result      = rr |> RssReader.addTwitterUser name
          in result |> Result

      | "remove" :: name :: _ ->
          rr |> RssReader.tryRemoveSource name |> Result

      | "rename" :: oldName :: newName :: _ ->
          rr |> RssReader.renameSource oldName newName |> Result

      | "sources" :: _ ->
          Source.allAtomicSources (rr |> RssReader.ctx) |> SourceSeq

      | "tag" :: tagName :: srcName :: _ ->
          rr |> RssReader.addTag tagName srcName |> Result

      | "detag" :: tagName :: srcName :: _ ->
          rr |> RssReader.removeTag tagName srcName |> Result

      | "tags" :: srcName :: _ ->
          Source.tagsOf (rr |> RssReader.ctx) srcName
          |> Seq.map (Source.ofTag)
          |> SourceSeq

      | "tags" :: _ ->
          Source.allTags (rr |> RssReader.ctx)
          |> Seq.map (Source.ofTag)
          |> SourceSeq

      | _ ->
          UnknownCommand command

  member this.ProcCommand(command) =
    lockConsole (fun () -> this.ProcCommandImpl(command))

  member this.ProcCommandLine(kont, lineOrNull) =
    async {
      match lineOrNull with
      | null | "" ->
          return! kont
      | "quit" | "halt" | "exit" ->
          ()
      | line ->
          let command =
            line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
          let result = this.ProcCommand(command)
          do! sendResult result
          return! kont
    }
