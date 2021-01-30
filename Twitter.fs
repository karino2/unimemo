module Twitter

open System
open System.IO
open FSharp.Data
open System.Text.RegularExpressions
open  Common

// fsharplint:disable Hints

let twitterDataPath = $"{unimemoRoot}/original/Twitter/data"


let toImagePath fname =
  sprintf "../../../original/Twitter/data/tweet_media/%s" fname



//
// 配列としてロード出来るtemp_tweet.jsonを作る
// 

let tweetPath = sprintf "%s/tweet.js" twitterDataPath
let writeToTemp outname (lines: seq<string>) =
    use writer = new StreamWriter(outname, false)
    writer.WriteLine("[ {")
    lines
    |> Seq.skip 1
    |> Seq.iter writer.WriteLine

// ダウンロードしたら一回目だけ実行。
let setupTempJson () =
    writeToTemp "temp_tweet.json" (File.ReadLines(tweetPath))


type Image = {
  Url: string
  FName: string
}

type ImageMeta = {
    Id: int64
    Files: Image array
}

type Link = {
    ShortUrl: string
    ExpandedUrl: string
}

type TweetType =
| NormalTweet
| ImageTweet of ImageMeta
| LinkTweet of Link array

type Tweet = {
    Date: DateTime
    FullText: string
    Type: TweetType
}

let toDate sdate =
    let tweetDTTemplate = "ddd MMM dd HH:mm:ss +ffff yyyy"
    DateTime.ParseExact(sdate, tweetDTTemplate, Globalization.CultureInfo("en-US"))


type TweetParser= JsonProvider<"temp_tweet.json">

let murl2fname url =
    let murl = Uri url
    murl.Segments.[murl.Segments.Length - 1]

let marray2images (media:TweetParser.Media[]) = 
    media |> Array.map (fun m -> {Url = m.Url; FName=(murl2fname m.MediaUrl)})


let toTweetType (tweet:TweetParser.Root) =
    let url2link (one :TweetParser.Url)=
        {ShortUrl= one.Url; ExpandedUrl = one.ExpandedUrl }

    match tweet.Tweet.Entities.Media with
    |[||] ->
        match tweet.Tweet.Entities.Urls with
        |[||] -> NormalTweet
        | urls ->
            let links = urls |> Array.map url2link
            LinkTweet links
    | marr -> ImageTweet {Id = tweet.Tweet.Id; Files = (marray2images marr) }

let toTweet (tweet:TweetParser.Root) =
    let ttype = toTweetType tweet
    let dt = toDate tweet.Tweet.CreatedAt
    {Date = dt; FullText=tweet.Tweet.FullText; Type=ttype}

// キーは日付、valueはその日のtweetのarray。sortはされてない。
type TDict = Collections.Generic.IDictionary<DateTime, Tweet[]>

// 日付をキー、Tweet[]がvalueのdictを返す
let tweets2dict (tweets:TweetParser.Root[]) : TDict=
  tweets |> Array.map toTweet |> Array.groupBy (fun t-> t.Date.Date) |> dict

let dict2months (tdict:TDict) =
  tdict.Keys |> Seq.groupBy (fun dt -> DateTime(dt.Year, dt.Month, 1)) |> Seq.map (fun (dt, _) -> dt) |> Seq.sort |> Seq.toArray

// その月の最初の日のdtを渡すと、最初から最後の日までのdtのリストを返す
let month2days (month:DateTime) =
  let rec todays (cur:DateTime) (rest: DateTime list) =
    if month.Month = cur.Month then
      todays (cur.AddDays 1.0) (cur::rest)
    else
      rest
  todays month []
  |> List.rev

let tweet2content (tweet:Tweet) =
  match tweet.Type with
  | NormalTweet -> tweet.FullText
  | ImageTweet img ->
      img.Files
      |> Array.map (fun imeta-> imeta, sprintf "%d-%s" img.Id imeta.FName)
      |> Array.map (fun (imeta, fpath) -> imeta, toImagePath fpath)
      |> Array.map (fun (imeta, fpath) -> imeta.Url, (sprintf "\n![%s](%s)" imeta.FName fpath))
      |> Array.fold (fun (full:string) (url, expanded) -> full.Replace(url, expanded)) tweet.FullText
  | LinkTweet links ->
    let expandLink (text:string) (link:Link) =
      text.Replace(link.ShortUrl, sprintf "[%s](%s)" link.ExpandedUrl link.ExpandedUrl)
    links
    |> Array.fold expandLink tweet.FullText

let headSharpPat = Regex("^#", RegexOptions.Multiline)

let tweet2md (tweet:Tweet) =
  let dts = tweet.Date.ToString("yyyy-MM-dd HH:mm:ss")
  let cont = tweet2content tweet
  sprintf "*%s*  \n%s" dts (headSharpPat.Replace(cont, "&#35;"))


let daytweet2md (tweets: Tweet[]) =
  tweets
  |> Array.sortBy (fun a -> a.Date)
  |> Array.map tweet2md
  |> String.concat "\n\n"
  

let mdMonthDI (dt:DateTime) =
  sprintf "%s/md/%s" mdroot (dt.ToString("yyyy/MM"))
  |> DirectoryInfo

let dt2fname (dt:DateTime) =
  sprintf "twitter_%s.md" (dt.ToString "yyyy_MM_dd")

let convone (tdict:TDict) (dt:DateTime) =
  match tdict.TryGetValue(dt) with
  | (true, tweets) ->
    let di = mdMonthDI dt
    ensureDir di
    let fi = FileInfo( Path.Combine(di.FullName, (dt2fname dt) ))
    let body = daytweet2md tweets
    let cont = sprintf "### %s\n\n%s" (dt.ToString "yyyy-MM-dd のツイート")  body
    saveText fi cont
  | _ ->
    ()

let convall (tdict:TDict) =
  tdict.Keys |> Seq.iter (convone tdict)
