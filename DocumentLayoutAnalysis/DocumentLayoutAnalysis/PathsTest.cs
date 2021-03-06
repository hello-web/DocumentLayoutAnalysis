﻿using Accord.Math.Geometry;
using ImageConverter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Geometry;
using static UglyToad.PdfPig.Geometry.PdfPath;
using System.Linq;
using Accord;
using Accord.Statistics.Visualizations;
using Accord.Controls;
using DocumentLayoutAnalysis.StraightSkeleton;

namespace DocumentLayoutAnalysis
{
    public class PathsTest
    {
        public static void Run(string path)
        {
            // check shape, see http://www.aforgenet.com/articles/shape_checker/
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker() { };

            float zoom = 20;
            var pinkPen = new Pen(Color.HotPink, zoom * 0.4f);
            var greenPen = new Pen(Color.GreenYellow, zoom * 0.7f);
            var aquaPen = new Pen(Color.Aqua, zoom * 0.7f);
            var redPen = new Pen(Color.Red, zoom * 0.4f);
            var bluePen = new Pen(Color.Blue, zoom * 0.4f);
            var blackPen = new Pen(Color.Black, zoom * 0.7f);

            using (var converter = new PdfImageConverter(path))
            using (PdfDocument document = PdfDocument.Open(path))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                    var paths = page.ExperimentalAccess.Paths;
                    var geometries = paths.Select(p => new PdfGeometry(p)).ToList();

                    var verticals = geometries.Where(g => g.IsVerticalLine()).ToList();

                    var horizontals = geometries.Where(g => g.IsHorizontalLine()).ToList();


                    using (var bitmap = converter.GetPage(i + 1, zoom))
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        var imageHeight = bitmap.Height;

                        foreach (var letter in page.Letters)
                        {
                            var rect = new Rectangle(
                                (int)(letter.GlyphRectangle.Left * (decimal)zoom),
                                imageHeight - (int)(letter.GlyphRectangle.Top * (decimal)zoom),
                                (int)(letter.GlyphRectangle.Width * (decimal)zoom),
                                (int)(letter.GlyphRectangle.Height * (decimal)zoom));
                            graphics.DrawRectangle(pinkPen, rect);
                        }

                        foreach (var p in paths)
                        {
                            if (p == null) continue;
                            PdfGeometry geometry = new PdfGeometry(p);

                            var isClosed = geometry.IsClosed;
                            var isClockwise = geometry.IsClockwise;

                            var commands = p.Commands;
                            var points = ToOrderedPoints(commands);

                            if (isClosed) //.SubGeometries.Count > 1)
                            {
                                //Scatterplot scatterplot = new Scatterplot();
                                //scatterplot.Compute(
                                //    points.Select(po => (double)po.X).Take(31).ToArray(),
                                //    points.Select(po => (double)po.Y).Take(31).ToArray(),
                                //    Enumerable.Range(0, points.Count).Take(31).ToArray());
                                //ScatterplotBox.Show(scatterplot);


                                ScatterplotView view = new ScatterplotView();
                                view.Dock = System.Windows.Forms.DockStyle.Fill;
                                view.LinesVisible = true;
                                view.Graph.GraphPane.Title.Text = isClockwise ? "CW" : "CCW";

                                foreach (var command in commands)
                                {
                                    if (command is PdfPath.Line line)
                                    {
                                        view.Graph.GraphPane.GraphObjList.Add(new ZedGraph.ArrowObj(
                                            Color.Blue, 10.0f, (double)line.From.X, (double)line.From.Y,
                                            (double)line.To.X, (double)line.To.Y));

                                        view.Graph.GraphPane.AddCurve("",
                                            new[] { (double)line.From.X, (double)line.To.X },
                                            new[] { (double)line.From.Y, (double)line.To.Y },
                                            Color.Red);
                                    }
                                    else if (command is BezierCurve curve)
                                    {
                                        foreach (var lineB in BezierCurveToPaths(curve))
                                        {
                                            view.Graph.GraphPane.GraphObjList.Add(new ZedGraph.ArrowObj(
                                                                      Color.Blue, 10.0f, (double)lineB.From.X, (double)lineB.From.Y,
                                                                      (double)lineB.To.X, (double)lineB.To.Y));

                                            view.Graph.GraphPane.AddCurve("",
                                                new[] { (double)lineB.From.X, (double)lineB.To.X },
                                                new[] { (double)lineB.From.Y, (double)lineB.To.Y },
                                                Color.Red);
                                        }
                                    }
                                }


                                //view.Graph.GraphPane.AddCurve("curve",
                                //    points.Select(po => (double)po.X).ToArray(),
                                //    points.Select(po => (double)po.Y).ToArray(),
                                //    Color.Blue,
                                //    ZedGraph.SymbolType.Circle);
                                view.Graph.GraphPane.AxisChange();
                                var f1 = new System.Windows.Forms.Form();
                                f1.Width = 1000;
                                f1.Height = 1000;
                                f1.Controls.Add(view);
                                f1.ShowDialog();
                            }

                            var shape = shapeChecker.CheckShapeType(points);
                            var subType = shapeChecker.CheckPolygonSubType(points);

                            var bboxF = GetBoundingRectangle(commands);
                            if (bboxF.HasValue)
                            {
                                var rect = new Rectangle(
                                    (int)(bboxF.Value.Left * (decimal)zoom),
                                    imageHeight - (int)(bboxF.Value.Top * (decimal)zoom),
                                    (int)(bboxF.Value.Width == 0 ? 1 : bboxF.Value.Width * (decimal)zoom),
                                    (int)(bboxF.Value.Height == 0 ? 1 : bboxF.Value.Height * (decimal)zoom));

                                graphics.DrawRectangle(greenPen, rect);
                            }

                            /*foreach (var command in commands)
                            {
                                if (command is PdfPath.Line line)
                                {
                                    var bbox = line.GetBoundingRectangle();
                                    if (bbox.HasValue)
                                    {
                                        var rect = new Rectangle(
                                            (int)(bbox.Value.Left * (decimal)zoom),
                                            imageHeight - (int)(bbox.Value.Top * (decimal)zoom),
                                            (int)(bbox.Value.Width == 0 ? 1 : bbox.Value.Width * (decimal)zoom),
                                            (int)(bbox.Value.Height == 0 ? 1 : bbox.Value.Height * (decimal)zoom));
                                        graphics.DrawRectangle(bluePen, rect);
                                    }
                                }
                                else if (command is BezierCurve curve)
                                {
                                    var bbox = curve.GetBoundingRectangle();
                                    if (bbox.HasValue)
                                    {
                                        var rect = new Rectangle(
                                            (int)(bbox.Value.Left * (decimal)zoom),
                                            imageHeight - (int)(bbox.Value.Top * (decimal)zoom),
                                            (int)(bbox.Value.Width == 0 ? 1 : bbox.Value.Width * (decimal)zoom),
                                            (int)(bbox.Value.Height == 0 ? 1 : bbox.Value.Height * (decimal)zoom));
                                        graphics.DrawRectangle(redPen, rect);
                                    }
                                }
                                else if (command is Close close)
                                {
                                    var bbox = close.GetBoundingRectangle();
                                    if (bbox.HasValue)
                                    {
                                        var rect = new Rectangle(
                                            (int)(bbox.Value.Left * (decimal)zoom),
                                            imageHeight - (int)(bbox.Value.Top * (decimal)zoom),
                                            (int)(bbox.Value.Width == 0 ? 1 : bbox.Value.Width * (decimal)zoom),
                                            (int)(bbox.Value.Height == 0 ? 1 : bbox.Value.Height * (decimal)zoom));
                                        graphics.DrawRectangle(greenPen, rect);
                                    }
                                }
                                else if (command is Move move)
                                {
                                    var bbox = move.GetBoundingRectangle();
                                    if (bbox.HasValue)
                                    {
                                        var rect = new Rectangle(
                                            (int)(bbox.Value.Left * (decimal)zoom),
                                            imageHeight - (int)(bbox.Value.Top * (decimal)zoom),
                                            (int)(bbox.Value.Width == 0 ? 1 : bbox.Value.Width * (decimal)zoom),
                                            (int)(bbox.Value.Height == 0 ? 1 : bbox.Value.Height * (decimal)zoom));
                                        graphics.DrawRectangle(greenPen, rect);
                                    }
                                }
                                else
                                {
                                    throw new NotImplementedException(command.GetType().ToString());
                                }
                            }*/
                        }

                        var rectsPaths = RecursiveXYCutPath.Instance.GetBlocks(paths, 0, 10, 10);
                        foreach (var rectPath in rectsPaths)
                        {
                            var rect = new Rectangle(
                                (int)(rectPath.Left * (decimal)zoom),
                                imageHeight - (int)(rectPath.Top * (decimal)zoom),
                                (int)(rectPath.Width * (decimal)zoom),
                                (int)(rectPath.Height * (decimal)zoom));
                            graphics.DrawRectangle(aquaPen, rect);
                        }

                        bitmap.Save(Path.ChangeExtension(path, (i + 1) + "_pathsTest.png"));
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of points from the curve: start point, end point and approximated intermediate points.
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        internal static List<PdfPoint> CubicBezierCurve(BezierCurve curve)
        {
            // https://ocw.mit.edu/courses/electrical-engineering-and-computer-science/6-837-computer-graphics-fall-2012/lecture-notes/MIT6_837F12_Lec01.pdf
            // https://en.wikipedia.org/wiki/B%C3%A9zier_curve

            List<PdfPoint> points = new List<PdfPoint>();
            points.Add(curve.StartPoint);
            points.Add(curve.EndPoint);

            Func<BezierCurve, double, PdfPoint> P_t = (bezierCurve, t) => // with 0 <= t <= 1
            {
                var x = (1 - t) * (1 - t) * (1 - t) * (double)bezierCurve.StartPoint.X +
                        3 * t * (1 - t) * (1 - t) * (double)bezierCurve.FirstControlPoint.X +
                        3 * t * t * (1 - t) * (double)bezierCurve.SecondControlPoint.X +
                        t * t * t * (double)bezierCurve.EndPoint.X;

                var y = (1 - t) * (1 - t) * (1 - t) * (double)bezierCurve.StartPoint.Y +
                        3 * t * (1 - t) * (1 - t) * (double)bezierCurve.FirstControlPoint.Y +
                        3 * t * t * (1 - t) * (double)bezierCurve.SecondControlPoint.Y +
                        t * t * t * (double)bezierCurve.EndPoint.Y;

                return new PdfPoint(x, y);
            };

            points.Add(P_t(curve, 0.20));
            points.Add(P_t(curve, 0.40));
            points.Add(P_t(curve, 0.60));
            points.Add(P_t(curve, 0.80));
            return points;
        }

        /// <summary>
        /// Returns a list of points from the curve: start point, end point and approximated intermediate points.
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        internal static List<PdfPath.Line> BezierCurveToPaths(BezierCurve curve)
        {
            // https://ocw.mit.edu/courses/electrical-engineering-and-computer-science/6-837-computer-graphics-fall-2012/lecture-notes/MIT6_837F12_Lec01.pdf
            // https://en.wikipedia.org/wiki/B%C3%A9zier_curve

            List<PdfPath.Line> lines = new List<PdfPath.Line>();

            Func<BezierCurve, double, PdfPoint> P_t = (bezierCurve, t) => // with 0 <= t <= 1
            {
                var x = (1 - t) * (1 - t) * (1 - t) * (double)bezierCurve.StartPoint.X +
                        3 * t * (1 - t) * (1 - t) * (double)bezierCurve.FirstControlPoint.X +
                        3 * t * t * (1 - t) * (double)bezierCurve.SecondControlPoint.X +
                        t * t * t * (double)bezierCurve.EndPoint.X;

                var y = (1 - t) * (1 - t) * (1 - t) * (double)bezierCurve.StartPoint.Y +
                        3 * t * (1 - t) * (1 - t) * (double)bezierCurve.FirstControlPoint.Y +
                        3 * t * t * (1 - t) * (double)bezierCurve.SecondControlPoint.Y +
                        t * t * t * (double)bezierCurve.EndPoint.Y;

                return new PdfPoint(x, y);
            };

            var point20 = P_t(curve, 0.20);
            var point40 = P_t(curve, 0.40);
            var point60 = P_t(curve, 0.60);
            var point80 = P_t(curve, 0.80);

            lines.Add(new PdfPath.Line(curve.StartPoint, point20));
            lines.Add(new PdfPath.Line(point20, point40));
            lines.Add(new PdfPath.Line(point40, point60));
            lines.Add(new PdfPath.Line(point60, point80));
            lines.Add(new PdfPath.Line(point80, curve.EndPoint));
            return lines;
        }

        internal static List<IntPoint> ToOrderedPoints(IReadOnlyList<IPathCommand> commands)
        {
            if (commands.Count == 0) return null;
            var lines = commands.Where(c => c is PdfPath.Line).Select(c => (PdfPath.Line)c).ToList();
            var curves = commands.Where(c => c is BezierCurve);

            lines.AddRange(curves.SelectMany(c => BezierCurveToPaths((BezierCurve)c)));

            List<PdfPoint> pdfPoints = new List<PdfPoint>();

            List<PdfPath.Line> orderedLines = new List<PdfPath.Line>();
            List<Vertex> vertices = new List<Vertex>();

            var current = lines[0];
            pdfPoints.Add(current.From);
            orderedLines.Add(current);

            while (true)
            {
                pdfPoints.Add(current.To);

                lines.Remove(current);
                if (lines.Count() == 0) break;

                var candidates = lines.Where(line => current.To.Equals(line.From));
                if (candidates.Count() == 0)
                {
                    current = lines[0];
                    pdfPoints.Add(current.From);
                    orderedLines.Add(current);
                    continue;
                }
                else
                {
                    current = candidates.First();
                    orderedLines.Add(current);
                }
            }

            /*if (orderedLines.Count > 3)
            {
                for (int l = 0; l < orderedLines.Count; l++)
                {
                    if (l == 0)
                    {
                        vertices.Add(new Vertex(orderedLines.First(), orderedLines.Last()));
                    }
                    else if (l == orderedLines.Count-1)
                    {
                        vertices.Add(new Vertex(orderedLines.Last(), orderedLines.First()));
                    }
                    else
                    {
                        vertices.Add(new Vertex(orderedLines[l], orderedLines[l + 1]));
                    }
                    if (vertices.Last().IsValid) pdfPoints.Add(vertices.Last().AngleBisectorRay.To);
                }
            }*/

            //if (pdfPoints.Count > 50)
            //{
            //    pdfPoints = RamerDouglasPeucker.Reduce(pdfPoints.ToArray(), 0.1).ToList();
            //}

            return pdfPoints.Select(p => new IntPoint((int)(p.X * 1000), (int)(p.Y * 1000))).ToList();
        }

        internal static List<IntPoint> ToPoints(IReadOnlyList<IPathCommand> commands)
        {
            if (commands.Count == 0) return null;

            List<PdfPoint> pdfPoints = new List<PdfPoint>();

            foreach (var command in commands)
            {
                if (command is PdfPath.Line line)
                {
                    if (!pdfPoints.Contains(line.From)) pdfPoints.Add(line.From);
                    if (!pdfPoints.Contains(line.To)) pdfPoints.Add(line.To);
                }
                else if (command is BezierCurve curve)
                {
                    var bezierPoints = CubicBezierCurve(curve);
                    foreach (var point in bezierPoints)
                    {
                        if (!pdfPoints.Contains(point)) pdfPoints.Add(point);
                    }
                }
                else if (command is Close close)
                {

                }
                else if (command is Move move)
                {

                }
                else
                {
                    throw new NotImplementedException(command.GetType().ToString());
                }
            }

            //if (pdfPoints.Count > 50)
            //{
            //    pdfPoints = RamerDouglasPeucker.Reduce(pdfPoints.ToArray(), 0.1).ToList();
            //}

            return pdfPoints.Select(p => new IntPoint((int)(p.X * 1000), (int)(p.Y * 1000))).ToList();
        }

        internal static PdfRectangle? GetBoundingRectangle(IReadOnlyList<IPathCommand> commands)
        {
            if (commands.Count == 0)
            {
                return null;
            }

            var minX = decimal.MaxValue;
            var maxX = decimal.MinValue;

            var minY = decimal.MaxValue;
            var maxY = decimal.MinValue;

            foreach (var command in commands)
            {
                var rect = command.GetBoundingRectangle();
                if (rect == null)
                {
                    continue;
                }

                if (rect.Value.Left < minX)
                {
                    minX = rect.Value.Left;
                }

                if (rect.Value.Right > maxX)
                {
                    maxX = rect.Value.Right;
                }

                if (rect.Value.Bottom < minY)
                {
                    minY = rect.Value.Bottom;
                }

                if (rect.Value.Top > maxY)
                {
                    maxY = rect.Value.Top;
                }
            }

            return new PdfRectangle(minX, minY, maxX, maxY);
        }
    }
}
