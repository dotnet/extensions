import { join } from 'path';
import { program } from 'commander';
import { cp, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'fs';
import packageJson from './package.json' with { type: 'json' };

program.requiredOption('--name <package-name>', 'The name of the package to copy');
program.requiredOption('--dest-root <path|property>', 'The root path of the destination folder, relative to the current directory, OR the name of a property defined in the "destroot" object in the package.json config section.');
program.option('--src-root <path>', 'The root path of the folder to copy from, relative to the node_modules folder');
program.requiredOption('--files <files>', 'A semicolon-separated list of files or directories to copy, relative to the src root folder');
program.parse();

const {
  name,
  srcRoot,
  destRoot,
  files,
} = program.opts();

// Normalize the destination root path by first checking if it's defined as a property in the package.json config section.
// If it's not, then we'll treat it as a path.
const normalizedDestRoot = packageJson.config.destRoot?.[destRoot] || destRoot;
if (!existsSync(normalizedDestRoot)) {
  logError('The specified destination root path does not exist:', normalizedDestRoot);
  process.exit(1);
}

// Infer package path from the specified package name.
const srcRootPath = join('node_modules', name, srcRoot || '');
if (!existsSync(srcRootPath)) {
  logError('The package root path does not exist:', srcRootPath);
  console.log(`Is the '${name}' package installed?`);
  process.exit(1);
}

// Log the package name so it's clear which dependency is being copied.
logSeparator();
console.log('📦', name);
logSeparator();

// Create a folder for the package in teh specified destination root, if it doesn't exist.
const destRootPath = join(normalizedDestRoot, name);
if (!existsSync(destRootPath)) {
  logInfo('No existing destination package folder found:', destRootPath);
  logWait('Creating the destination folder...');
  mkdirSync(destRootPath, { recursive: true });
  logSuccess('Folder created successfully!');
  logSeparator();
}

// Get the version of the package and update the README to include the package name and version.
const version = await findPackageVersion(name);
updateReadmeHeader(name, version, destRootPath);
logSeparator();

// Finally, copy the specified files from the package to the destination folder.
copyDependencies(srcRootPath, destRootPath, files);

async function findPackageVersion(packageName) {
  const packageJsonPath = './' + join('node_modules', packageName, 'package.json');
  let packageJson;
  try {
    packageJson = await import(packageJsonPath, { with: { type: 'json' } });
  } catch (error) {
    logWarn('Could not find the package.json file at the specified path:', packageJsonPath);
    return null;
  }

  const packageVersion = packageJson.default.version;
  if (!packageVersion) {
    logWarn('The package.json file does not contain a version field:', packageJsonPath);
    return null;
  }

  logSuccess(`Found ${packageName} package version:`, packageVersion);
  return packageVersion;
}

function updateReadmeHeader(packageName, packageVersion, destRootPath) {
  let newHeader;
  if (packageVersion) {
    newHeader = `${packageName} version ${packageVersion}`;
  } else {
    logWarn('The updated README will not include the package version.');
    newHeader = packageName;
  }

  const readmeFilePath = join(destRootPath, 'README.md');
  if (existsSync(readmeFilePath)) {
    logInfo('Existing README detected:', readmeFilePath);
    logWait('Updating the README header...');
    const readmeContent = readFileSync(readmeFilePath, 'utf8');
    const newReadmeContent = readmeContent.replace(/^(.*)$/m, newHeader);
    writeFileSync(readmeFilePath, newReadmeContent);
    logSuccess('README header updated successfully!');
  } else {
    logWait('Creating a new README file:', readmeFilePath);
    writeFileSync(readmeFilePath, newHeader);
    logSuccess('README created successfully!');
  }
}

function copyDependencies(srcRootPath, destRootPath, files) {
  const distFolderPath = join(destRootPath, 'dist');
  if (existsSync(distFolderPath)) {
    logInfo('Found an existing dist folder:', distFolderPath);
    logWait('Clearing existing dist folder contents...');
    rmSync(distFolderPath, { recursive: true, force: true });
    logSuccess('Contents cleared successfully!');
  } else {
    logWait('Creating a new dist folder: ', distFolderPath);
    mkdirSync(distFolderPath, { recursive: true });
    logSuccess('Folder created successfully!');
  }

  logWait('Copying files...');
  for (const file of files.split(';')) {
    const srcPath = join(srcRootPath, file);
    const destPath = join(distFolderPath, file);
    cp(srcPath, destPath, { recursive: true }, (error) => {
      if (error) {
        logError(`An error occurred while copying ${srcPath}:`, error);
      } else {
        logSuccess(srcPath, '->', destPath);
      }
    });
  }
}

function logSeparator() {
  console.log('\n================\n');
}

function logInfo(message, ...optionalParams) {
  console.log('ℹ️', message, ...optionalParams);
}

function logSuccess(message, ...optionalParams) {
  console.log('✅', message, ...optionalParams);
}

function logWait(message, ...optionalParams) {
  console.log('⌛', message, ...optionalParams);
}

function logWarn(message, ...optionalParams) {
  console.error('⚠️', message, ...optionalParams);
}

function logError(message, ...optionalParams) {
  console.error('❌', message, ...optionalParams);
}
