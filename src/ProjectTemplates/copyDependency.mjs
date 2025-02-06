import { join } from 'path';
import { program } from 'commander';
import { cp, existsSync, mkdirSync, readFileSync, rm, writeFileSync } from 'fs';
import packageJson from './package.json' with { type: 'json' };

program.requiredOption('--name <package-name>', 'The name of the package to copy');
program.requiredOption('--dest-root <path|property>', 'The root path of the destination folder, relative to the current directory, OR the name of a property defined in the "destroot" object in the package.json config section.');
program.option('--src-root <path>', 'The root path of the folder to copy from, relative to the node_modules folder');
program.option('--filter <files>', 'A semicolon-separated list of files or directories to copy, relative to the src-root folder');
program.parse();

const {
  name,
  srcRoot,
  destRoot,
  filter,
} = program.opts();

const normalizedDestRoot = packageJson.config.destRoot?.[destRoot] || destRoot;
if (!existsSync(normalizedDestRoot)) {
  console.error('The specified destination root path does not exist:', normalizedDestRoot);
  process.exit(1);
}

const srcRootPath = join('node_modules', name, srcRoot || '');
if (!existsSync(srcRootPath)) {
  console.error('The package root path does not exist:', srcRootPath);
  console.error(`Is the '${name}' package installed?`);
  process.exit(1);
}

const destRootPath = join(normalizedDestRoot, name);
if (!existsSync(destRootPath)) {
  console.log('Creating the destination folder:', destRootPath);
  mkdirSync(destRootPath, { recursive: true });
}

const version = await findPackageVersion(name);
updateReadmeHeader(name, version, destRootPath);

const distFolderPath = join(destRootPath, 'dist');
if (existsSync(distFolderPath)) {
  console.log('Found an existing dist folder:', distFolderPath);
  console.log('Clearing its contents...');
  rm(distFolderPath, { recursive: true, force: true }, (error) => {
    if (error) {
      console.error('An error occurred while deleting the dist folder:', error);
    } else {
      console.log('Dist folder cleared successfully!');
    }
  });
} else {
  console.log('Creating a new dist folder: ', distFolderPath);
  mkdirSync(distFolderPath, { recursive: true });
  console.log('Folder created successfully!');
}

if (filter) {
  for (const file of filter.split(';')) {
    const srcPath = join(srcRootPath, file);
    const destPath = join(distFolderPath, file);
    cp(srcPath, destPath, { recursive: true }, (error) => {
      if (error) {
        console.error('An error occurred while copying the file:', error);
      } else {
        console.log('File copied successfully:', srcPath, '->', destPath);
      }
    });
  }
} else {
  // Handle this case.
}

async function findPackageVersion(packageName) {
  const packageJsonPath = './' + join('node_modules', packageName, 'package.json');
  let packageJson;
  try {
    packageJson = await import(packageJsonPath, { with: { type: 'json' } });
  } catch (error) {
    console.error('Could not find the package.json file at the specified path:', packageJsonPath);
    return null;
  }

  const packageVersion = packageJson.default.version;
  if (!packageVersion) {
    console.error('The package.json file does not contain a version field:', packageJsonPath);
    return null;
  }

  console.log('Found package version:', packageVersion);
  return packageVersion;
}

function updateReadmeHeader(packageName, packageVersion, destRootPath) {
  let newHeader;
  if (packageVersion) {
    newHeader = `${packageName} version ${packageVersion}`;
  } else {
    console.error('The updated README will not include the package version.');
    newHeader = packageName;
  }

  const readmeFilePath = join(destRootPath, 'README.md');
  if (existsSync(readmeFilePath)) {
    console.log('Existing README detected:', readmeFilePath);
    console.log('Updating its header...');
    const readmeContent = readFileSync(readmeFilePath, 'utf8');
    const newReadmeContent = readmeContent.replace(/^(.*)$/m, newHeader);
    writeFileSync(readmeFilePath, newReadmeContent);
    console.log('README header updated successfully!');
  } else {
    console.log('Creating a new README file:', readmeFilePath);
    writeFileSync(readmeFilePath, newHeader);
    console.log('README created successfully!');
  }
}
