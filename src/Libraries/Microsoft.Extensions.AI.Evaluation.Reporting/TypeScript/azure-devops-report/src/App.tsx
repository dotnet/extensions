// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export const App = ({ html }: { html?: string }) => {
  if (html) {
    html = `data:text/html,${encodeURIComponent(html)}`;
  }
  return (
    <iframe src={html}/>
  );
}
