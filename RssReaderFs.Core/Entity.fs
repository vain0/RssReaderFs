﻿namespace RssReaderFs.Core

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<AutoOpen>]
module Entity =
  [<AllowNullLiteral>]
  type EntityWithId() =
    [<Key>]
    member val Id = 0L with get, set

  [<AllowNullLiteral>]
  type Article() =
    inherit EntityWithId()

    [<Required>]
    member val Title = "" with get, set

    member val DescOrNull = (null: string) with get, set

    member val LinkOrNull = (null: string) with get, set

    [<Required; Index>]
    member val Date = DateTime.Now with get, set

    [<Required; Index>]
    member val SourceId = 0L with get, set

    member this.Desc
      with get () = this.DescOrNull |> Option.ofObj
      and  set v  =
        this.DescOrNull <- v |> Option.toObj

    member this.Link
      with get () = this.LinkOrNull |> Option.ofObj
      and  set v  =
        this.LinkOrNull <- v |> Option.toObj

  [<AllowNullLiteral>]
  type ReadLog() =
    [<Key; DatabaseGenerated(DatabaseGeneratedOption.None)>]
    member val ArticleId = 0L with get, set

    [<Required>]
    member val Date = DateTime.Now with get, set

  [<AllowNullLiteral>]
  type Source() =
    inherit EntityWithId()

    [<Required; Index(IsUnique = true)>]
    member val Name = "" with get, set

  [<AllowNullLiteral>]
  type TwitterUser() =
    [<Key; DatabaseGenerated(DatabaseGeneratedOption.None)>]
    member val SourceId = 0L with get, set

    member val SinceId = 0L with get, set

  [<AllowNullLiteral>]
  type RssFeed() =
    [<Key; DatabaseGenerated(DatabaseGeneratedOption.None)>]
    member val SourceId = 0L with get, set

    [<Required; Index(IsUnique = true)>]
    member val Url = "" with get, set

  [<AllowNullLiteral>]
  type Tag() =
    [<Key; DatabaseGenerated(DatabaseGeneratedOption.None)>]
    member val SourceId = 0L with get, set

  [<AllowNullLiteral>]
  type TagToSource() =
    inherit EntityWithId()

    [<Required; Index>]
    member val TagId = 0L with get, set

    [<Required; Index>]
    member val SourceId = 0L with get, set

  [<AllowNullLiteral>]
  type BearTokenCache() =
    inherit EntityWithId()

    [<Required>]
    member val BearToken = "" with get, set
