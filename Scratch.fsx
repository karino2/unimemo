// #r "bin/Debug/net5.0/FSharp.Data.DesignTime.dll"
// #r "bin/Debug/net5.0/FSharp.Data.dll"
#r "nuget: FSharp.Data"
#load "Common.fs"
#load "Twitter.fs"

open System
open Twitter
// fsharplint:disable Hints


// ダウンロードした一回目だけ実行
setupTempJson ()
let tweets = TweetParser.Load(__SOURCE_DIRECTORY__ + "/temp_tweet.json")

// let tweets = TweetParser.GetSamples()

let tdict = tweets2dict tweets

convall tdict

// 一つだけコンバートするテスト
convone tdict (DateTime(2014, 3, 13))
convone tdict (DateTime(2014, 3, 8))


