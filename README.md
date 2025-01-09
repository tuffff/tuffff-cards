![tuffCards Logo](https://github.com/tuffff/tuffff-cards/blob/main/res/icon.svg?raw=true)

tuffCards is a small tool to convert html/css/csv templates to cards.

# Quick Start
Installation: `choco install tuff-cards`\
Command overview: `tuffCards --help`\
See example output:
```
md example-project | cd
tuffCards create-example
tuffCards convert
.\output\default\actions.html
.\output\default\buildings.html
```


# General workflow
- Prepare an empty project folder
- Create a project with `tuffCards create`
- Add all needed card types with `tuffCards add-type <name>` - this will add a matching csv, html and css file in `/cards`
- Adjust the csv to include all needed fields
- Adjust the html to render those fields
- Create the layout via css
- Convert the project with `tuffCards convert`
- Take a look at your cards in `/output/default` - just open the file in your browser
- To export your cards in a specific format, copy `/targets/default.html` to a new file and adjust, then convert with `--target <target>`
- To create an image from your target, use `--image` - you may need to adjust the size (see below)
- For a continued editing workflow, keep both the default html open and the terminal with `tuffCards convert --watch`. Each written change in one of the read files (not new ones though) should be visible in the browser relatively quickly). This requires JS permission for local files, which may not be enabled in your browser. Please check your browser console if you run into problems.
- If you want to import your cards into [Tabletop Simulator (TTS)](https://www.tabletopsimulator.com/), the `sprite` is already there. Convert with `tuffCards --target sprite --image` and import into a card deck in TTS; you just need to set the amount of total cards and the number of horizontal cards (10).

# How it works
tuffCards does three or four templating steps:

## Parse Markdown from `/cards/<type>.csv`
The table is expected to be a csv (I default to semicolon-separated) file that represents the card data. This is parsed with [Sep](https://github.com/nietras/Sep), with `Unescape=true`.
The header-line defines the name of the fields (use alphanumeric and `-` or `_`). By default, each content-row will result in one card.

The content of each field is parsed as Markdown with [Markdig](https://github.com/xoofx/markdig). There are two custom commands:
- `{iconname}` will result in an image with the css-class `icon` and the first file from the `/icons` folder in the format `iconname.anything`. If no file is found, `iconname` will just show up (and a warning appears in your terminal).
- `{{imagename}}` will do the same, but with the css-class `image` and from the folder `/images`.

There are some special header names that get additional handling in addition to being available in the template:
- `Copies` means the content is parsed as a number and the card in this line is added that many times.
- `Deck` splits one file in multiple decks, of course each using the same template.

This action is done for every csv file found in `/cards`. A matching html template is expected and throws an error if missing.

## Parse card template from `/cards/<type>.html`
The html file is parsed with [scriban](https://github.com/scriban/scriban). 
- Use the format `{{ columnname }}` to insert the content from the card table. Use the name exactly as in the header-line in the csv file.
- Use the function `md` to also perform a markdown parse with the same rules as above. `{{ md "{iconname}" }}` would result in a static icon.

## Parse target template from `/targets/<target>.html`
This step packs all cards of one type into a single file. If not specified, `/targets/default.html` is used. The following fields can be used:
- `name` is the card type's name. This can be useful to target specific card types via global css.
- `cards` is a list of all cards of that type.
- `scripts` contains all scripts from `/scripts`. By default, the script `/scripts/fit-text` can be used to shrink text to fit. These files are optional.
- `globaltargetcss` is just the file `/targets/global.css`. This file is optional.
- `cardtypecss` is the file `/cards/<type>.css`. This file is optional.

The result is written to `/output/<target>/<type>.html`.

## (Optional) Render the target as image
If you use the `--image` option, tuffCards uses [Puppeteer Sharp](https://github.com/hardkoded/puppeteer-sharp) to take a to take a screenshot.
