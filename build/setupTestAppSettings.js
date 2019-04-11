// The goal of this file is to enable us to programmatically set the Razor OmniSharp plugin path. Typically when project system changes
// happen we need to wait until OmniSharp consumes those changes to properly test them. This file enables us to locate the plugin and
// then setup the test apps to use the new plugin path.

var fs = require('fs');
var path = require('path');

function findInDir(directoryPath, needle) {
    if (!fs.existsSync(directoryPath)){
        return;
    }

    var files = fs.readdirSync(directoryPath);
    for (var i = 0; i < files.length; i++){
        var filename = path.join(directoryPath, files[i]);

        if (fs.lstatSync(filename).isDirectory()){
            var result = findInDir(filename, needle);
            if (result) {
                return result;
            }
        } else if (filename.indexOf(needle) >= 0) {
            return filename;
        };
    };
}

console.log('Reading existing .vscode/settings.json for test apps...');
var vscodeSettingsPath = '../test/testapps/.vscode/settings.json';
var bytes = fs.readFileSync(vscodeSettingsPath);
var json = JSON.parse(bytes);

console.log('Locating OmniSharp plugin dll...')
var relativePath = findInDir('../src/Microsoft.AspNetCore.Razor.OmniSharpPlugin/bin', 'Microsoft.AspNetCore.Razor.OmniSharpPlugin.dll');

if (!relativePath) {
    console.warn('Warning: Could not locate OmniSharp plugin path. Falling back to installed plugin.');
} else {
    var absolutePath = path.resolve(relativePath);
    console.log(`Found Razor plugin dll path as '${absolutePath}'.`);
    json["razor.plugin.path"] = absolutePath;
}

console.log('Saving testapps vscode settings.');
var stringifiedJson = JSON.stringify(json, /* replacer */ null, /* spaces */ 4);
fs.writeFileSync(vscodeSettingsPath, stringifiedJson);