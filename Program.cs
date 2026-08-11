using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System;
using System.Windows;
using System.Diagnostics;



try
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    //DirectoryInfo debugDir = new DirectoryInfo(@"/mnt/MagNet_Portable_HDD_1T/智赛/耦合项目/耦合Dome机/bin/Debug");
    //DirectoryInfo debugDir = new DirectoryInfo(@"C:\Users\Lenovo\Desktop\智赛\耦合项目\耦合Dome机\bin\Debug");
    DirectoryInfo debugDir = new DirectoryInfo(System.Environment.CurrentDirectory).Parent!;

    if (debugDir!.Exists == false)
    {
        Console.WriteLine("找不到调试目录");
        goto end;
    }

    DirectoryInfo projectsHomeDir = new DirectoryInfo(Path.Combine(debugDir.FullName, "Projects"));
    if (projectsHomeDir.Exists == false)
    {
        Console.WriteLine($"找不到项目目录: {projectsHomeDir.FullName}");
        goto end;
    }


    FileInfo mainConfigFile = new FileInfo(Path.Combine(debugDir.FullName, "Config.xml"));
    if (mainConfigFile.Exists == false)
    {
        Console.WriteLine($"找不到配置文件: {mainConfigFile.FullName}");
        goto end;
    }



    var line = ReadAllLines(mainConfigFile.FullName, Encoding.GetEncoding("gb2312")).FirstOrDefault(l => l.Contains("活动工程名称"));
    if (line is null)
    {
        Console.WriteLine($"在配置文件中找不到活动工程名称: {mainConfigFile.FullName}");
        goto end;
    }

    Regex regex = new Regex(@"<ProjectName[^>]*>(.*?)</ProjectName>");
    Match match = regex.Match(line);

    if (match.Success == false || match.Groups.Count < 1)
    {
        Console.WriteLine($"在配置文件中找不到活动工程名称: {mainConfigFile.FullName}");
        goto end;
    }

    string projectName = match.Groups[1].Value;


    DirectoryInfo projectDir = new DirectoryInfo(Path.Combine(projectsHomeDir.FullName, projectName));
    if (projectDir.Exists == false)
    {
        Console.WriteLine($"找不到项目目录: {projectDir.FullName}");
        goto end;
    }


    string tab = "&#9;";
    string enter = "&#10;\r\n";

    List<string> oneLineItems = new List<string>();


    string file = string.Empty;
    try
    {
        file = Path.Combine(projectDir.FullName, "Alarm", "Alarm.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var allInfo = root
                .EnumerateArray()
                .Select(i => (key: i.GetProperty("Key"), msg: i.GetProperty("Msg")));

            oneLineItems.AddRange(allInfo.Select(t => $"#{t.key}"));
            oneLineItems.AddRange(allInfo.Select(t => $"#{t.key}{tab}'{t.msg}（报警）"));
            oneLineItems.AddRange(allInfo.Select(t => $"alarm#{t.key}{tab}'{t.msg}"));
        }
    }
    catch(Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }


    IEnumerable<(string key, string name)> axisKeyName = Enumerable.Empty<(string key, string name)>();
    try
    {
        file = Path.Combine(projectDir.FullName, "Axis", "Axis.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;

            var allInfo = root.EnumerateArray().Select(i => i.GetProperty("AxisPrm"))
                .Select(p => (tag: p.GetProperty("Tag").ToString(), name: p.GetProperty("Name").ToString()));


            axisKeyName = root.EnumerateArray().Select(i => i.GetProperty("AxisPrm"))
                    .Select(p => (key: p.GetProperty("Key").ToString(), name: p.GetProperty("Name").ToString()));

            oneLineItems.AddRange(allInfo.Select(t => $"{t.name}"));
            oneLineItems.AddRange(allInfo.Select(t => $"{t.tag}{tab}'{t.name}"));
            oneLineItems.AddRange(allInfo.Select(t => $"axis-{t.tag}{tab}'{t.name}"));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }


    try
    {
        file = Path.Combine(projectDir.FullName, "Communication", "Comm.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var allInfo = root.GetProperty("$values").EnumerateArray().Select(i => (key: i.GetProperty("Key").ToString(), name: i.GetProperty("Prm").GetProperty("Name").ToString()));

            oneLineItems.AddRange(allInfo.Select(i => $"Comm[{i.key}]{tab}'{i.name}"));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }


    try
    {
        file = Path.Combine(projectDir.FullName, "IO", "Cylinder.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var allInfo = root.EnumerateArray().Select(i => (key: i.GetProperty("Key").ToString(), name: i.GetProperty("Name").ToString()));

            oneLineItems.AddRange(allInfo.Select(i => i.name));
            oneLineItems.AddRange(allInfo.Select(i => "cylinder-" + i.name));
            oneLineItems.AddRange(allInfo.Select(i => "cc-" + i.name));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }


    try
    {
        file = Path.Combine(projectDir.FullName, "IO", "X.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var xGroup = Enumerable.Range(0, 1000)
                .Where(i => root.TryGetProperty(i.ToString(), out _))
                .Select(i => (head: i.ToString(), body:
                      Enumerable.Range(0, 16)
                        .Where(n => root.GetProperty(i.ToString()).GetProperty($"In_{n}").ToString() != string.Empty)
                        .Select(n => (end: n, name: root.GetProperty(i.ToString()).GetProperty($"In_{n}").GetProperty("Prm").GetProperty("Name").ToString()))))
                        .Where(g => g.body.Any());

            foreach (var t in xGroup)
            {
                foreach (var l in t.body)
                {
                    oneLineItems.Add($"X{t.head}.{l.end}{tab}'{l.name}");
                    oneLineItems.Add($"XX-{t.head}.{l.end}-{l.name}");
                    oneLineItems.Add($"X{t.head}.{l.end}");
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }



    try
    {
        file = Path.Combine(projectDir.FullName, "IO", "Y.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var xGroup = Enumerable.Range(0, 1000)
                .Where(i => root.TryGetProperty(i.ToString(), out _))
                .Select(i => (head: i.ToString(), body:
                      Enumerable.Range(0, 16)
                        .Where(n => root.GetProperty(i.ToString()).GetProperty($"Out_{n}").ToString() != string.Empty)
                        .Select(n => (end: n, name: root.GetProperty(i.ToString()).GetProperty($"Out_{n}").GetProperty("Prm").GetProperty("Name").ToString()))))
                        .Where(g => g.body.Any());

            foreach (var t in xGroup)
            {
                foreach (var l in t.body)
                {
                    oneLineItems.Add($"Y{t.head}.{l.end}{tab}'{l.name}");
                    oneLineItems.Add($"YY-{t.head}.{l.end}-{l.name}");
                    oneLineItems.Add($"Y{t.head}.{l.end}");
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }



    try
    {
        file = Path.Combine(projectDir.FullName, "Point", "Point.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var pGroup = Enumerable.Range(0, 10000)
                .Where(i => root.TryGetProperty(i.ToString(), out _))
                .Select(i => (pointHead: i.ToString(), body: root.GetProperty(i.ToString())))
                .Where(i => i.body.GetProperty("Remark").ToString() != "");

            List<(string pointName, string pointHead, string pointEnd, string axisKey)> reb = new List<(string pointName, string pointHead, string pointEnd, string axisKey)>();
            foreach (var gi in pGroup)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (gi.body.GetProperty($"AxisKey{i.ToString()}").ToString() != "0")
                    {
                        reb.Add((gi.body.GetProperty("Remark").ToString(), gi.pointHead, i.ToString(), gi.body.GetProperty($"AxisKey{i.ToString()}").ToString()));
                    }
                }
            }

            oneLineItems.AddRange(reb.Select(r => $"point-P{r.pointHead}-{r.pointName}").Distinct());
            oneLineItems.AddRange(reb.Select(r => $"pp-P{r.pointHead}-{r.pointName}").Distinct());
            foreach (var r in reb)
            {
                var axisName = axisKeyName.FirstOrDefault(i => i.key == r.axisKey).name;
                if (string.IsNullOrEmpty(axisName) == false)
                {
                    oneLineItems.Add($"P{r.pointHead}.{r.pointEnd}");
                    oneLineItems.Add($"P{r.pointHead}.{r.pointEnd}{tab}'{r.pointName}-{axisName}");
                }
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }



    try
    {
        file = Path.Combine(projectDir.FullName, "POPWin", "POPWin.mgr");
        if (File.Exists(file))
        {
            var allText = ReadAllText(file);
            var root = JsonDocument.Parse(allText).RootElement;
            var allInfo = root
                .EnumerateArray()
                .Select(i => (key: i.GetProperty("Key"), msg: i.GetProperty("Msg")));

            oneLineItems.AddRange(allInfo.Select(t => $"#{t.key}"));
            oneLineItems.AddRange(allInfo.Select(t => $"#{t.key}{tab}'{t.msg}（弹窗）"));
            oneLineItems.AddRange(allInfo.Select(t => $"form-#{t.key}{tab}'{t.msg}"));
            oneLineItems.AddRange(allInfo.Select(t => $"ff-#{t.key}{tab}'{t.msg}"));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }




    try
    {
        file = Path.Combine(projectDir.FullName, "PrjConfig.ini");
        if (File.Exists(file))
        {
            var allData = ReadAllLines(file, Encoding.GetEncoding("gb2312"))
                .Where(l => l.StartsWith("#"))
                .Select(l => l.Trim().Split('='))
                .Where(a => a.Length == 2);

            oneLineItems.AddRange(allData.Select(d => $"{d[0]}"));
            oneLineItems.AddRange(allData.Select(d => $"{d[0]}{tab}'{d[1]}（配置）"));
            oneLineItems.AddRange(allData.Select(d => $"para-{d[0]}{tab}'{d[1]}"));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }


    List<(string name, string[]? para, string? note)> functions = new List<(string name, string[]? para, string? note)>();
    List<(string name, string[]? para, string? note)> structs = new List<(string name, string[]? para, string? note)>();
    try
    {
        file = Path.Combine(projectDir.FullName, "Prg", "common.bas");
        if (File.Exists(file))
        {
            var allLile = ReadAllLines(file).Select(l => l.Trim()).ToArray();

            for (int i = 0; i < allLile.Length; i++)
            {
                if (allLile[i].Trim().StartsWith("Function"))
                {
                    var noHead = allLile[i].Trim().Replace("Function ", "").Replace(")", "");
                    if (string.IsNullOrEmpty(noHead))
                        continue;

                    var name_Para_ar = noHead.Split('(');
                    if (name_Para_ar.Length < 2)
                        continue;
                    (string name, string[]? para, string? note) funcInfo = ("", null, null);
                    funcInfo.name = name_Para_ar[0];
                    if (string.IsNullOrEmpty(name_Para_ar[1]) == false)
                        funcInfo.para = name_Para_ar[1].Split(',').Select(p => p.Trim()).ToArray();

                    List<string> notes = new List<string>();
                    for (int j = i - 1; j > 0; j--)
                    {
                        var text = allLile[j].Trim();
                        if (text.StartsWith("'"))
                        {
                            notes.Add(text);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (notes.Any())
                    {
                        funcInfo.note = string.Join(enter, notes);
                    }

                    functions.Add(funcInfo);
                }
            }


            for (int i = 0; i < allLile.Length; i++)
            {
                if (allLile[i].Trim().StartsWith("Struct "))
                {
                    var noHead = allLile[i].Trim().Replace("Struct ", "").Replace(")", "");
                    if (string.IsNullOrEmpty(noHead))
                        continue;

                    var name_Para_ar = noHead.Split('(');
                    if (name_Para_ar.Length < 2)
                        continue;
                    (string name, string[]? para, string? note) structInfo = ("", null, null);
                    structInfo.name = name_Para_ar[0];
                    if (string.IsNullOrEmpty(name_Para_ar[1]) == false)
                        structInfo.para = name_Para_ar[1].Split(',').Select(p => p.Trim()).ToArray();

                    List<string> notes = new List<string>();
                    for (int j = i - 1; j > 0; j--)
                    {
                        var text = allLile[j].Trim();
                        if (text.StartsWith("'"))
                        {
                            notes.Add(text);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (notes.Any())
                    {
                        structInfo.note = string.Join(enter, notes);
                    }

                    structs.Add(structInfo);
                }
            }


        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"处理文件时出错: {file}\r\n{e.ToString}");
    }


    List<(string key, string note)> vars = new List<(string key, string note)>();

    var prgDir = Path.Combine(projectDir.FullName, "Prg");
    var files = Directory.GetFiles(prgDir).Where(fn => fn.EndsWith(".bas"));

    var allLines = files.SelectMany(f => ReadAllLines(f));
    vars = allLines
        .Where(l => l.Trim().StartsWith("'var "))
       .Select(l => l.Trim().Replace("'var ", ""))
       .Select(t => t.Split('=').Select(n => n.Trim()))
       .Where(t => t.Count() > 1 && t.All(t => !string.IsNullOrEmpty(t)))
       .Select(i => (key: i.First(), note: i.Skip(1).First()))
       .ToList();

    var defs =
    allLines
        .Where(l => l.Trim().StartsWith("'def "))
       .Select(l => l.Trim().Replace("'def ", ""));
    oneLineItems.AddRange(defs);
    oneLineItems.AddRange(defs.Select(t => $"def-{t}"));


    oneLineItems.AddRange(vars.Select(v => $"{v.key}"));
    oneLineItems.AddRange(vars.Select(v => $"{v.key}{tab}'{v.note}"));
    oneLineItems.AddRange(vars.Select(v => $"var-{v.key}{tab}'{v.note}"));


    var autoCompletionFile = Path.Combine(debugDir.FullName, "Notepad++", "autoCompletion", "FreeBasic.xml");
    if (File.Exists(autoCompletionFile))
    {
        var allLine = ReadAllLines(autoCompletionFile).ToList();
        var startIndex = allLine.FindIndex(l => l.Trim() == "<!-- 自动生成区域起始 -->");
        var endindex = allLine.FindIndex(l => l.Trim() == "<!-- 自动生成区域结束 -->");

        if (startIndex > 0 && endindex > 0)
        {
            allLine.RemoveRange(startIndex + 1, endindex - startIndex - 1);
            startIndex = allLine.FindIndex(l => l.Trim() == "<!-- 自动生成区域起始 -->");
            endindex = allLine.FindIndex(l => l.Trim() == "<!-- 自动生成区域结束 -->");

            var insertIndex = startIndex + 1;
            allLine.InsertRange(insertIndex, oneLineItems.Select(li => $"<KeyWord name=\"{li}\" />"));


            foreach (var func in functions)
            {
                List<string> buffer = new List<string>();
                buffer.Add("<KeyWord");
                buffer.Add($"\tname=\"{func.name}\"");
                buffer.Add("\tfunc=\"yes\">");
                buffer.Add("<Overload");
                buffer.Add($"\t\tretVal=\"\"");
                buffer.Add($"\t\tdescr=\"{func.note ?? ""}\">");
                if (func.para == null || func.para.Length < 1)
                    buffer.Add("\t\t<Param name=\"\" />");
                else
                    buffer.AddRange(func.para.Select(p => $"\t\t<Param name=\"{p}\" />"));

                buffer.Add("\t</Overload>");
                buffer.Add("</KeyWord>");

                allLine.InsertRange(insertIndex, buffer);
            }


            foreach (var _struct in structs)
            {
                List<string> buffer = new List<string>();
                buffer.Add($"<KeyWord name=\"struct-{_struct.name}{tab}{_struct.note}\" />");
                buffer.Add("<KeyWord");
                buffer.Add($"\tname=\"{_struct.name}\"");
                buffer.Add("\tfunc=\"yes\">");
                buffer.Add("<Overload");
                buffer.Add($"\t\tretVal=\"\"");
                buffer.Add($"\t\tdescr=\"{_struct.note ?? ""}\">");
                if (_struct.para == null || _struct.para.Length < 1)
                    buffer.Add("\t\t<Param name=\"\" />");
                else
                    buffer.AddRange(_struct.para.Select(p => $"\t\t<Param name=\"{p}\" />"));

                buffer.Add("\t</Overload>");
                buffer.Add("</KeyWord>");

                allLine.InsertRange(insertIndex, buffer);
            }

            File.WriteAllLines(autoCompletionFile, allLine);
        }
    }
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}

if (File.Exists("notepad++.exe"))
{
    Process[] processes = Process.GetProcessesByName("notepad++");

    if (processes.Length > 0)
    {
        Console.WriteLine($"正在重启notepad++...");
        foreach (var p in processes)
        {
            p.Kill();           // 强制结束进程
            p.WaitForExit();    // 等待进程完全退出
        }
    }
    else
    {
        Console.WriteLine("未找到 Notepad++ 进程。");
    }

    Console.WriteLine("");
    Console.WriteLine("axis-轴");
    Console.WriteLine("struct-结构体");
    Console.WriteLine("alarm-报警");
    Console.WriteLine("form-弹窗\tff-弹窗");
    Console.WriteLine("para-配置, 界面上的配置");
    Console.WriteLine("cylinder-气缸\tcc-气缸");
    Console.WriteLine("point-点位\tPP-点位");
    Console.WriteLine("var-自定义变量, 定义格式如下, 在任意代码程序中声明, 但是建议还是放在comm中.");
    Console.WriteLine("\t'var #1000 = 变量名称");

    Console.WriteLine("def-自定义字段, 定义格式如下, 在任意代码程序中声明");
    Console.WriteLine("\t'def 任意的文字,但是不要包含空格,例如一些代码片段或者是说明书");

    Console.WriteLine("");
    Console.WriteLine("按任意键继续...");
    Console.ReadKey();

    string notepadPath = @"notepad++.exe";
    Process.Start(notepadPath);
}


end:
Console.WriteLine("end");



static string ReadAllText(string path, Encoding? enc = null)
{
    try
    {
        if (enc is null)
            return File.ReadAllText(path);
        else
            return File.ReadAllText(path, enc);
    }
    catch
    {
        return "";
    }
}


static string[] ReadAllLines(string path, Encoding? enc = null)
{
    try
    {
        if (enc is null)
            return File.ReadAllLines(path);
        else
            return File.ReadAllLines(path, enc);
    }
    catch
    {
        return Enumerable.Empty<string>().ToArray();
    }
}