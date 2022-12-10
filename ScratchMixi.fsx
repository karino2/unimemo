// fsharplint:disable Hints

#r "nuget: HtmlAgilityPack"
#r "nuget: HtmlAgilityPack.CssSelectors.NetCore"

// EUC用
#r "nuget: System.Text.Encoding.CodePages"

#load "Common.fs"
#load "Mixi.fs"

open Common

open Mixi
open Mixi.Download
open HtmlAgilityPack

open HtmlAgilityPack.CssSelectors.NetCore
open System.Text.RegularExpressions

//
// 更新
//

//
// diary更新手順
//
// 1. unimemo/list_diaryをmkdirする。
// 2. mixi_get.shを更新
// 3.以下を実行

downloadListDiaryYear 2021
// 4. 手動で mixi/list_diary/ 下にコピー

// 5. 以下を実行
getOneYearDiary 2021

// 6. diary下を手動でコピー

// 7. 以下を実行
open System.IO
open Mixi.ConvertDiary

convYearDiary 2021


//
// ボイスの更新手順
//

// 1. unimemo/list_voiceとunimemo/view_voiceをmkdir
// 2. mixi/list_voice から最後のタイムスタンプを取得
// 3. 以下の最後の引数を1のタイムスタンプに置き換えて実行
saveLV "https://mixi.jp/list_voice.pl" "20210211125019"
// 4. list_vliceを移動
// 5. view_voiceから最後のタイムスタンプをとって、以下の最後の引数を置き換えて実行
// なお以下は「もっとコメントを見る　」があるものだけダウンロードする（それ以外はlist_voiceの方にすべて入っているので）
saveAllVoices "20210115014748"
// 6. view_voice以下を移動


//
// 新規ダウンロード
//
// ただしこのあと更新時に引数を追加したのでそのままでは動かない。記録のため残す。
// もし新しくやり直す必要があるならすごく昔のタイムスタンプとかをlastに指定すれば良いはず。

// list_diary.plのダウンロード

downloadListDiaryMonthList 2004 [10..12]

[2005..2020] |> List.map downloadListDiaryYear

downloadListDiaryMonth 2021 1

// 手動で mixi/list_diary/ 下にコピー

// diaryのダウンロード
getOneLDDiary 2004 10 1
getOneLDDiary 2004 11 1
getOneLDDiary 2004 12 1

getOneYearDiary 2005
[2006..2021] |> List.iter getOneYearDiary


// ボイスのlvのダウンロード

saveLV "https://mixi.jp/list_voice.pl"


// ボイスのmoreのある物だけダウンロード
saveAllVoices ()


//
// コンバート
//

// diary のコンバート
open System.IO
open Mixi.ConvertDiary




convYearDiary 2004
convYearDiary 2005
convYearDiary 2006

[2007..2021]
|> List.map convYearDiary

open Mixi.ConvertVoice

convAllVList ()

//
// アドホックな試行錯誤
//

