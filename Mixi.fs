module Mixi

open Common
open System
open System.IO
open System.Text

open HtmlAgilityPack
open HtmlAgilityPack.CssSelectors.NetCore
open System.Text.RegularExpressions

// fsharplint:disable Hints

// EUC対応
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)


let mixiOrigPath = $"{unimemoRoot}/original/mixi"
let mixiMdPath = $"{mdroot}/md"

let loadHtml (path:string) =
    let doc = HtmlDocument()
    doc.Load(path, Encoding.GetEncoding("EUC-JP"))
    doc

module Download =

    // pageは1が無いと同じ。
    let listDiaryUrl year month page =
        let burl = $"https://mixi.jp/list_diary.pl?year={year}&month={month}"
        if page = 1 then
            burl
        else
            $"{burl}&page={page}"

    let listFileName year month page = $"list_diary_{year}_{month}_{page}.html"

    let mixiGetPath = $"{mixiOrigPath}/mixi_get.sh"

    let mixiGet url fname =
        shellExecute mixiGetPath $"{url} {fname}" |> ignore

    let downloadListDiaryOne year month page =
        let url = listDiaryUrl year month page
        let fname = listFileName year month page
        let fpath = $"list_diary/{fname}"
        mixiGet url fpath
        fpath

    let seqAHref (doc:HtmlDocument) =
        doc.QuerySelectorAll("a")
       |> Seq.map (fun a -> a.GetAttributeValue("href", ""))

    // patはグループが1つ、Group.[1].Valueのseqを返す。
    let seqMatches (pat:Regex) (hrefs: string seq) =
        hrefs
       |> Seq.filter  (fun href -> pat.Match(href).Success)
       |> Seq.map (fun href -> pat.Match(href).Groups.[1].Value)

    let seqAHrefMatches (pat:Regex) (doc:HtmlDocument) =
        seqAHref doc
        |> seqMatches pat

    let pagePat = Regex(".*list_diary.pl?.*page=([0-9]+).*")

    let hasNext (doc:HtmlDocument) (page:int)=
        let allA = seqAHrefMatches pagePat doc
                    |> Seq.map (fun pageId -> System.Int32.Parse(pageId))
                    |> Seq.filter (fun num -> num > page)
                    |> Seq.toList 
        allA.Length <>0

    open System.Threading
    let waitTime = 2000

    let downloadListDiaryMonth year month =
        let rec downloadRec (fs: string list) page =
            Thread.Sleep(waitTime)
            let fname = downloadListDiaryOne year month page
            printfn $"Download {fname}"
            let doc = loadHtml fname
            let newFs = fname::fs
            if not (hasNext doc page) then
                newFs
            else
                downloadRec newFs (page+1)
        downloadRec [] 1

    let downloadListDiaryMonthList year monthList =
        monthList
        |> List.map (downloadListDiaryMonth year)

    let downloadListDiaryYear year =
        downloadListDiaryMonthList year [1..12]



    // 各日記のダウンロード
    open System.IO

    let ownerIdPat = Regex("https://mixi\.jp/view_diary\.pl\?.*owner_id=([0-9]+).*")

    let getOwnerId (ldDoc:HtmlDocument) =
        seqAHrefMatches ownerIdPat ldDoc
        |> Seq.head

    let editPat = Regex("https://mixi\.jp/edit_diary\.pl\?id=([0-9]+).*")

    let seqDiaryIds (ldDoc:HtmlDocument) =
        seqAHrefMatches editPat ldDoc

    let seqViewDiaryUrls (ownerId:string) (ldDoc:HtmlDocument) =
        let idSeq = seqDiaryIds ldDoc
        idSeq
        |> Seq.map (fun id-> (id, $"https://mixi.jp/view_diary.pl?id={id}&owner_id={ownerId}&full=1"))

    let diaryDir year month page =
        let fpath = $"diary/{year}/%02d{month}_{page}"
        let di = DirectoryInfo(fpath)
        ensureDir di
        di

    let getOneLDDiary year month page =
        let fname = listFileName year month page
        let destDI = diaryDir year month page
        let ldpath = $"{unimemoRoot}/original/mixi/list_diary/{fname}"
        let ldoc = loadHtml ldpath
        let ownerId = getOwnerId ldoc
        seqViewDiaryUrls ownerId ldoc
        |> Seq.iter (fun (id, url)->
                let dest = Path.Combine(destDI.FullName, $"{id}.html")
                mixiGet url dest
                Thread.Sleep(waitTime)
                )

    let listLD year =
        let di = DirectoryInfo  $"{mixiOrigPath}/list_diary/"
        di.GetFiles()
        |> Array.filter (fun fi -> fi.Name.StartsWith($"list_diary_{year}"))

    let ldPat = Regex("list_diary_\d*_(\d*)_(\d*).html")

    let ld2monthPage (fi:FileInfo) =
        let m = ldPat.Match(fi.Name)
        if not m.Success then
            failwith($"listdiary not match(never happen): {fi.FullName}")
        System.Int32.Parse(m.Groups.[1].Value), System.Int32.Parse(m.Groups.[2].Value)

    let getOneYearDiary year =
        listLD year
        |> Array.map ld2monthPage
        |> Array.iter (fun (month, page)->
            getOneLDDiary year month page)

    // ボイスのダウンロード

    let nextHrefLV (lvdoc:HtmlDocument) =
        lvdoc.QuerySelector(".pageList02").QuerySelectorAll("a") |> Seq.map (fun a->a.GetAttributeValue("href", ""))
        |> Seq.filter (fun href-> href.Contains("direction=next"))
        |> Seq.toList
        |> Seq.tryHead

    let lvBPTPat = Regex("list_voice\.pl\?.*base_post_time=([0-9]+).*")

    let saveLV startUrl lastBpt =
        let rec saveRec url bpt =
            if bpt <= lastBpt then
                ()
            else
                let path = $"list_voice/{bpt}.html"
                printfn $"Downlaoding: {path}"
                Thread.Sleep(waitTime)
                mixiGet url path
                let lvdoc = loadHtml path
                match nextHrefLV lvdoc with
                | Some href->
                    let m = lvBPTPat.Match(href)
                    let nextBpt = m.Groups.[1].Value
                    saveRec $"https://mixi.jp/{href}" nextBpt
                | _-> ()
        
        let now = DateTime.Now
        let nowBPT = now.ToString("yyyyMMddHHmmss")

        saveRec startUrl nowBPT


    let voicePTPat = Regex("https://mixi.jp/view_voice\.pl\?.*post_time=([0-9]+).*")

    let saveVoices lastpt (lvdoc:HtmlDocument) =
        lvdoc.QuerySelectorAll(".moreLink01.hrule a") 
        |> Seq.map (fun a->a.GetAttributeValue("href", ""))
        |> Seq.map (fun href-> 
                    voicePTPat.Match(href).Groups.[1].Value, href)
        |> Seq.toList
        |> List.iter (fun (pt, url) ->
            if pt > lastpt then
                let fname = $"view_voice/{pt}_vv.html"
                printfn $"Downloading {fname}"
                Thread.Sleep(waitTime)
                mixiGet url fname)

    let listLV () =
        let di = DirectoryInfo  $"{mixiOrigPath}/list_voice/"
        di.GetFiles()
        |> Array.filter (fun fi -> fi.Name.EndsWith($"html"))

    let saveAllVoices lastpt =
        listLV ()
        |> Array.map (fun fi -> loadHtml fi.FullName)
        |> Array.map (saveVoices lastpt)


module ConvertDiary =
    type MonthPage = {
        Month: string
        Page: string
    }

    let monthDI2monthPagePair (di:DirectoryInfo) =
        match di.Name.Split("_") with
        | [|month; page|] -> Some {Month=month; Page=page }
        | _ -> None

    let listMP year =
        let di = DirectoryInfo($"{mixiOrigPath}/diary/{year}/")
        di.EnumerateDirectories("*")
        |> Seq.map monthDI2monthPagePair
        |> Seq.choose id
        |> Seq.toList

    let title (ddoc: HtmlDocument) =
        let div = ddoc.QuerySelector(".listDiaryTitle")
        let ttext = div.QuerySelector("dt").FirstChild.InnerText
        let tdate = div.QuerySelector(".date").FirstChild.InnerText
        $"### {tdate} {ttext}"



    let node2text (node:HtmlNode) =
        match node with
        | :? HtmlTextNode as tnode -> tnode.Text
        | _ ->
            if node.Name = "br" then
                "  \n"
            else
                node.OuterHtml

    let children2md (nodes : HtmlNode seq) =
        nodes
        |> Seq.map node2text
        |> String.concat ""

    let body (ddoc:HtmlDocument) =
        ddoc.QuerySelector("#diary_body").ChildNodes
        |> children2md


    let comdl2comment (comdl: HtmlNode) =
        let uname = comdl.QuerySelector("dt a").InnerText
        let dtext = comdl.QuerySelector("dt span").InnerText.Replace("&nbsp;", " ")
        let children = comdl.ChildNodes |> Seq.toArray
        let comment = children.[3].ChildNodes |> children2md
        // sprintf "### %s %s\n%s" dtext uname comment
        $"**{dtext} {uname}**\n\n{comment}"

    let dlcomments2md (coms: HtmlNode seq) =
        coms
        |> Seq.map comdl2comment
        |> String.concat "\n\n"

    let comments (ddoc:HtmlDocument) =
        ddoc.QuerySelectorAll("dl.comment")
        |> dlcomments2md


    let ddoc2md (ddoc:HtmlDocument) =
        let ttext = title ddoc
        let btext = body ddoc
        let tcomments = comments ddoc
        sprintf "%s\n%s\n\n----\n**comments**\n\n%s" ttext btext tcomments



    let diarymdFname year mp idstr =
        $"mixid_{year}_{mp.Month}_{mp.Page}_{idstr}.md"



    let convOneMPDiary year mp =
        let destDI = DirectoryInfo $"{mixiMdPath}/{year}/{mp.Month}"
        ensureDir destDI
        let di = DirectoryInfo $"{mixiOrigPath}/diary/{year}/{mp.Month}_{mp.Page}"
        di.EnumerateFiles()
        |> Seq.filter (fun fi->fi.Name.EndsWith(".html"))
        |> Seq.map (fun fi-> (fi.Name, (loadHtml fi.FullName)))
        |> Seq.map (fun (fname, ddoc) ->
                let idstr = fname.Substring(0, fname.LastIndexOf(".html"))
                let fname = diarymdFname year mp idstr
                let fpath = Path.Combine(destDI.FullName, fname)
                let md = ddoc2md ddoc
                saveText (FileInfo fpath) md
            )
        |> Seq.toList
        |> ignore

    let convYearDiary year =
        listMP year
        |> List.map (convOneMPDiary year)


module ConvertVoice =

    let vw2voice (vw:HtmlNode) =
        let vchildren = vw.QuerySelector(".voiced").FirstChild.ChildNodes
        // 最後の子供は時刻を表すspan
        let body = [0..(vchildren.Count-2)]
                    |> List.map (fun i -> vchildren.[i].InnerText)
                    |> String.concat " "
        let tdate = vchildren.[vchildren.Count-1].FirstChild.InnerText
        sprintf "%s \n**%s**" body tdate

    let vw2comments (vw:HtmlNode) =
        vw.QuerySelectorAll(".commentRow.hrule")
        |> Seq.map (fun crow -> 
                    let com = crow.QuerySelector ".commentBody"
                    let children = com.ChildNodes
                    // last -2 date span
                    // last-1 delete
                    // last empty
                    let body = [0..(children.Count-4)]
                                |> List.map (fun i -> children.[i].InnerText)
                                |> String.concat " "
                    let tdate = children.[children.Count - 3].InnerText
                    let nick = crow.QuerySelector(".commentNickName").InnerText
                    sprintf "**%s**  %s**%s**" nick body tdate
                    )
        |> String.concat "\n\n"

    let vw2md (vw:HtmlNode) =
        let body = vw2voice vw
        let comments = vw2comments vw
        sprintf "%s\n\n%s" body comments


    let lvdoc2md (lvdoc:HtmlDocument) =
        lvdoc.QuerySelectorAll(".voiceWrap")
        |> Seq.map vw2md
        |> String.concat "\n\n----\n\n"

    let lvfi2yearMonth (fi:FileInfo) =
        let year = fi.Name.Substring(0, 4)
        let month = fi.Name.Substring(4, 2)
        year, month

    let listLVFI () =
        let di = DirectoryInfo $"{mixiOrigPath}/list_voice"
        di.EnumerateFiles()
        |> Seq.filter (fun fi -> fi.Name.EndsWith(".html"))

    let convAllVList () =
        listLVFI ()
        |> Seq.map (fun fi -> (lvfi2yearMonth fi), fi)
        |> Seq.map (fun ((year, month), fi) ->
                    let id = Path.GetFileNameWithoutExtension fi.Name
                    let destDI = DirectoryInfo $"{mixiMdPath}/{year}/{month}"
                    ensureDir destDI
                    let destFI = FileInfo(Path.Combine(destDI.FullName, $"mixivl_{id}.md"))
                    let doc = loadHtml fi.FullName
                    let md = lvdoc2md doc
                    saveText destFI md
                )
        |> Seq.toList |> ignore

