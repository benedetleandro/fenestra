const fs = require("fs");
const path = require("path");

const root = process.argv[2] || ".";
const html = fs.readFileSync(path.join(root, "docs", "index.html"), "utf8");
const js = fs.readFileSync(path.join(root, "docs", "app.js"), "utf8");

const keys = [
  ...new Set(
    [...html.matchAll(/data-i18n(?:-html|-aria)?="([^"]+)"/g)].map((m) => m[1])
  )
];

const missing = keys.filter((k) => !js.includes('"' + k + '"'));
const ptBlock = js.slice(js.indexOf("var PT"), js.indexOf("var META"));
const ptKeys = [...ptBlock.matchAll(/"([a-z0-9]+\.[a-zA-Z0-9]+)":/g)].map((m) => m[1]);
const extra = [...new Set(ptKeys)].filter((k) => !keys.includes(k));

console.log("chaves no HTML:", keys.length);
console.log("sem traducao PT:", missing.length ? missing.join(", ") : "nenhuma");
console.log("traducoes orfas:", extra.length ? extra.join(", ") : "nenhuma");

process.exit(missing.length ? 1 : 0);
