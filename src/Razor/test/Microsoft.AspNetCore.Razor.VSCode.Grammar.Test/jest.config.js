// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

module.exports = {
  globals: {
    "ts-jest": {
      "tsConfig": "./tsconfig.json",
      "babeConfig": true,
      "diagnostics": true
    }
  },
  testPathIgnorePatterns: [ 'dist' ],
  preset: 'ts-jest',
  testEnvironment: 'jsdom'
};
