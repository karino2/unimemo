#!/usr/bin/env python
# -*- encoding: utf-8 -*-
import click
import os
import sys
from bs4 import BeautifulSoup
from datetime import datetime
import time
from markdownify import markdownify as md

def datetime_to_unixtime(dt):
    assert isinstance(dt, datetime)
    return time.mktime(dt.timetuple())


def touch(fname, atime=datetime.now(), mtime=datetime.now()):
    if not os.path.exists(fname):
        open(fname, 'a').close()
    os.utime(fname, (datetime_to_unixtime(atime), datetime_to_unixtime(mtime)))

def item2md(conts):
    md = []
    for item in conts:
        if item.name == "span" and item['class'][0] == 'bullet':
            pass # already handled in [x]
        elif item.name == "span" and item['class'][0] == 'text':
            md.append(item.text)
        else:
            pass
            # print("item2md: ", item.name, ":", item.text)
    return ''.join(md)

def list2md(ul):
    md = []
    for item in ul.contents:
        if item.name == "li":
            kls = item['class']
            if kls[0] == 'listitem':
                if len(kls) >= 2 and kls[1] == 'checked':
                    cont = item2md(item.contents)
                    line = f"- [x] ~~{cont}~~"
                    # print("li: line=", line)
                    md.append(line)
                else:
                    cont = item2md(item.contents)
                    line = f"- [ ] {cont}"
                    md.append(line)
            else:
                print("li: class=", item['class'])
                print("line=", f"* {item.text}")
                md.append(f"* {item.text}")
        else:
            pass
            # print(item.name, ": ", item.text)
    return '\n'.join(md)

def parse(fp):
    soup = BeautifulSoup(fp, 'lxml')

    note = soup.find('div', class_='note')

    title, timestamp, archived, content, labels = None, None, False, None, []

    for div in note.find_all('div'):
        class1 = div['class'][0]
        if class1 == 'heading':
            tstr = div.text.strip()
            timestamp = datetime.strptime(tstr, '%Y/%m/%d %H:%M:%S')
            # print 'TIMESTAMP (%s)' % timestamp
        elif class1 == 'archived':
            archived = True
            # print '+ARCHIVED'
        elif class1 == 'title':
            title = div.text.strip()
            # print 'TITLE (%s)' % title.encode('utf-8')
        elif class1 == 'content':
            # content = md(div.encode_contents())
            contents = []
            for content in div.contents:
                 if content.name == 'br':
                     contents.append('\n')
                 elif content.name == "ul" and content['class'][0] == "list":
                     mds = list2md(content)
                     # print("UL!: ", mds, "</UL>")
                     contents.append(mds)
                 else:
                     contents.append(content.text.rstrip())
                     # contents.append(md(content.encode_contents()))
            content = ''.join(contents)
            # print('CONTENT: ', content.encode('utf-8'))
            # print('CONTENT: ', content)
        elif class1 == 'labels':
            labels = [label.text for label in div.find_all('span', class_='label')]
            # print 'LABELS', labels
        else:
            # print class1, div
            pass

    return (title, timestamp, archived, content, labels)


@click.group()
def cli():
    pass


def conv(html_path):
    print('PATH:', html_path)
    with open(html_path, 'r') as fp:
        (title, timestamp, archived, content, labels) = parse(fp)
        print( 'TITLE:', title)
        print('TIMESTAMP:', timestamp)
        # print 'ARCHIVED:', archived
        if archived:
            labels.append('archived')
        # print content
        labels.append('keep')
        print( 'LABELS:', ' '.join(['#%s' % label for label in labels]))
        # print( 'DEB_CONTENT:', content)
    
    if html_path.endswith('.html'):
        md_path = html_path.replace('.html', '.md')
    else:
        md_path = html_path + '.md'

    with open(md_path, 'w') as fp:
        def _tagify(label):
            if ' ' in label:
                return '\\#%s\\#' % label
            else:
                return '#%s' % label
        if title:
            fp.write("# ")
            fp.write(title)
            fp.write('\n')
        if timestamp:
            fp.write("*")
            fp.write(str(timestamp))
            fp.write("*")
            fp.write('\n\n')
        fp.write(content)
        fp.write('\n')
        fp.write('\n')
        fp.write('%s\n' % ' '.join([_tagify(label) for label in labels]))

    touch(md_path, atime=timestamp, mtime=timestamp)


@cli.command()
@click.argument('html-path', type=click.Path('r'))
def one(html_path):
    assert os.path.exists(html_path)
    conv(html_path)


@cli.command()
@click.argument('html-dir', type=click.Path('r'), default='.')
def dir(html_dir):
    assert os.path.exists(html_dir) and os.path.isdir(html_dir)
    for path in os.listdir(html_dir):
        if path.endswith(".html"):
            full_path = os.path.join(html_dir, path)
            conv(full_path)


if __name__ == '__main__':
    cli()
