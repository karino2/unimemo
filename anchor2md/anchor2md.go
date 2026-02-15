package main

import (
    "encoding/xml"
    "fmt"
    "os"
    "time"
)

type RSS struct {
    Channel Channel `xml:"channel"`
}

type Channel struct {
    Title string  `xml:"title"`
    Items []Item `xml:"item"`
}

type Item struct {
    Title       string `xml:"title"`
    Link        string `xml:"link"`
    Description string `xml:"description"`
    PubDate     string `xml:"pubDate"`
}

func main() {
    if len(os.Args) < 2 {
        fmt.Println("使い方: anchor2md <rssファイルパス>")
        return
    }
    filePath := os.Args[1]

    file, err := os.Open(filePath)
    if err != nil {
        panic(err)
    }
    defer file.Close()

    var rss RSS
    if err := xml.NewDecoder(file).Decode(&rss); err != nil {
        panic(err)
    }

    for _, item := range rss.Channel.Items {
        fmt.Println("##", item.Title)
        fmt.Print("*")
        
        t, err := time.Parse(time.RFC1123Z, item.PubDate)
        if err != nil { 
            t, err = time.Parse(time.RFC1123, item.PubDate)
        }
        if err == nil { 
            fmt.Print(t.Local().Format("2006-01-02 15:04:05")) 
        } else { 
            fmt.Print(item.PubDate) 
        }
        fmt.Println("*")
        fmt.Println("")
        fmt.Println("```")
        fmt.Print(item.Description)
        fmt.Println("```")
        fmt.Println("")
    }
}
