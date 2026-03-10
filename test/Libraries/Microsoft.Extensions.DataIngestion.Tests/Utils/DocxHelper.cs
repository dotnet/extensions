// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Microsoft.Extensions.DataIngestion.Tests.Utils;

#pragma warning disable IDE0007 // Use implicit type

internal static class DocxHelper
{
    internal static Stream CreateDocumentWithTable(string[,] cells)
    {
        MemoryStream stream = new();

        using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            MainDocumentPart mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            // Add a header
            Paragraph headerPara = body.AppendChild(new Paragraph());
            Run headerRun = headerPara.AppendChild(new Run());
            headerRun.AppendChild(new Text("Key Milestones"));
            headerRun.RunProperties = new(new Bold());

            // Create table
            Table table = new Table();

            // Table properties
            TableProperties tableProps = new(
                new TableBorders(
                    new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 })
            );
            table.AppendChild(tableProps);

            // Create rows
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                TableRow row = new TableRow();

                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    TableCell cell = new TableCell();

                    // Cell properties
                    TableCellProperties cellProps = new(
                        new TableCellMargin(
                            new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                            new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                            new LeftMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                            new RightMargin { Width = "100", Type = TableWidthUnitValues.Dxa })
                    );
                    cell.AppendChild(cellProps);

                    // Cell content
                    Paragraph cellPara = new Paragraph();
                    Run cellRun = new Run();
                    cellRun.AppendChild(new Text(cells[i, j]));

                    // Make header row bold
                    if (i == 0)
                    {
                        cellRun.RunProperties = new(new Bold());
                    }

                    cellPara.AppendChild(cellRun);
                    cell.AppendChild(cellPara);
                    row.AppendChild(cell);
                }

                table.AppendChild(row);
            }

            body.AppendChild(table);
        }

        stream.Position = 0;
        return stream;
    }
}
