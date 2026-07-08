namespace SevenZip
{
#if SFX
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;

    using SfxSettings = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Sfx 模块选择枚举
    /// </summary>
    public enum SfxModule
    {
        /// <summary>
        /// 默认模块（不确定时请保留此项）
        /// </summary>
        Default,
        /// <summary>
        /// Igor Pavlov 编写的简单 sfx 模块，无可调参数
        /// </summary>
        Simple,
        /// <summary>
        /// Igor Pavlov 编写的安装程序 sfx 模块
        /// </summary>
        Installer,
        /// <summary>
        /// Oleg Scherbakov 编写的扩展安装程序 sfx 模块
        /// </summary>
        Extended,
        /// <summary>
        /// 自定义 sfx 模块。首先必须指定模块文件名。
        /// </summary>
        Custom
    }

    /// <summary>
    /// 用于制作基于 7-zip 的自解压归档的类。
    /// </summary>
    public class SevenZipSfx
    {
        private static Dictionary<SfxModule, List<string>> SfxSupportedModuleNames
        {
            get
            {
                var result = new Dictionary<SfxModule, List<string>>
                {
                    {SfxModule.Simple, new List<string>(2) {"7z.sfx", "7zCon.sfx"}},
                    {SfxModule.Installer, new List<string>(2) {"7zS.sfx", "7zSD.sfx"}}
                };

                if (Environment.Is64BitProcess)
                {
                    result.Add(SfxModule.Default, new List<string>(1) { "7zxSD_All_x64.sfx" });
                    result.Add(SfxModule.Extended, new List<string>(4) { "7zxSD_All_x64.sfx", "7zxSD_Deflate_x64", "7zxSD_LZMA_x64", "7zxSD_PPMd_x64" });
                }
                else
                {
                    result.Add(SfxModule.Default, new List<string>(1) { "7zxSD_All.sfx" });
                    result.Add(SfxModule.Extended, new List<string>(4) { "7zxSD_All.sfx", "7zxSD_Deflate", "7zxSD_LZMA", "7zxSD_PPMd" });
                }

                return result;
            }
        }

        private string _moduleFileName;
        private Dictionary<SfxModule, List<string>> _sfxCommands;

        /// <summary>
        /// 初始化 SevenZipSfx 类的新实例。
        /// </summary>
        public SevenZipSfx()
        {
            SfxModule = SfxModule.Default;
            CommonInit();
        }

        /// <summary>
        /// 初始化 SevenZipSfx 类的新实例。
        /// </summary>
        /// <param name="module">要用作前端的 sfx 模块。</param>
        public SevenZipSfx(SfxModule module)
        {
            if (module == SfxModule.Custom)
            {
                throw new ArgumentException("You must specify the custom module executable.", nameof(module));
            }

            SfxModule = module;
            CommonInit();
        }

        /// <summary>
        /// 初始化 SevenZipSfx 类的新实例。
        /// </summary>
        /// <param name="moduleFileName"></param>
        public SevenZipSfx(string moduleFileName)
        {
            SfxModule = SfxModule.Custom;
            ModuleFileName = moduleFileName;
            CommonInit();
        }

        /// <summary>
        /// 获取 sfx 模块类型。
        /// </summary>
        public SfxModule SfxModule { get; private set; }

        /// <summary>
        /// 获取或设置自定义 sfx 模块文件名
        /// </summary>
        public string ModuleFileName
        {
            get => _moduleFileName;

            set
            {
                if (!File.Exists(value))
                {
                    throw new ArgumentException("The specified file does not exist.");
                }

                _moduleFileName = value;
                SfxModule = SfxModule.Custom;
                var sfxName = Path.GetFileName(value);

                foreach (var mod in SfxSupportedModuleNames.Keys)
                {
                    if (SfxSupportedModuleNames[mod].Contains(sfxName))
                    {
                        SfxModule = mod;
                    }
                }
            }
        }

        private void CommonInit()
        {
            LoadCommandsFromResource("Configs");
        }

        private static string GetResourceString(string str)
        {
            return "SevenZip.sfx." + str;
        }

        /// <summary>
        /// 根据支持的模块列表获取 sfx 模块枚举
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static SfxModule GetModuleByName(string name)
        {
            if (name.Contains("7z.sfx", StringComparison.Ordinal))
            {
                return SfxModule.Simple;
            }
            if (name.Contains("7zS.sfx", StringComparison.Ordinal))
            {
                return SfxModule.Installer;
            }
            if (name.Contains("7zxSD_All.sfx", StringComparison.Ordinal))
            {
                return SfxModule.Extended;
            }
            throw new SevenZipSfxValidationException("The specified configuration is unsupported.");
        }

        private static readonly Assembly s_assembly = typeof(SevenZipSfx).Assembly;

        /// <summary>
        /// 为每个支持的 sfx 模块配置加载命令
        /// </summary>
        /// <param name="xmlDefinitions">xml 定义的资源名称</param>
        private void LoadCommandsFromResource(string xmlDefinitions)
        {
            using (var cfg = s_assembly.GetManifestResourceStream(
                GetResourceString(xmlDefinitions + ".xml")))
            {
                if (cfg == null)
                {
                    throw new SevenZipSfxValidationException("The configuration \"" + xmlDefinitions +
                                                             "\" does not exist.");
                }
                using (var schm = s_assembly.GetManifestResourceStream(
                    GetResourceString(xmlDefinitions + ".xsd")))
                {
                    if (schm == null)
                    {
                        throw new SevenZipSfxValidationException("The configuration schema \"" + xmlDefinitions +
                                                                 "\" does not exist.");
                    }
                    var sc = new XmlSchemaSet();
                    using (var scr = XmlReader.Create(schm))
                    {
                        sc.Add(null, scr);
                        var settings = new XmlReaderSettings {ValidationType = ValidationType.Schema, Schemas = sc};
                        var validationErrors = "";
                        settings.ValidationEventHandler +=
                            ((s, t) =>
                            {
                                validationErrors += string.Format(CultureInfo.InvariantCulture, "[{0}]: {1}\n",
                                                                  t.Severity.ToString(), t.Message);
                            });
                        using (var rdr = XmlReader.Create(cfg, settings))
                        {
                            _sfxCommands = new Dictionary<SfxModule, List<string>>();
                            rdr.Read();
                            rdr.Read();
                            rdr.Read();
                            rdr.Read();
                            rdr.Read();
                            rdr.ReadStartElement("sfxConfigs");
                            rdr.Read();
                            do
                            {
                                var mod = GetModuleByName(rdr["modules"]);
                                rdr.ReadStartElement("config");
                                rdr.Read();
                                if (rdr.Name == "id")
                                {
                                    var cmds = new List<string>();
                                    _sfxCommands.Add(mod, cmds);
                                    do
                                    {
                                        cmds.Add(rdr["command"]);
                                        rdr.Read();
                                        rdr.Read();
                                    } while (rdr.Name == "id");
                                    rdr.ReadEndElement();
                                    rdr.Read();
                                }
                                else
                                {
                                    _sfxCommands.Add(mod, null);
                                }
                            } while (rdr.Name == "config");
                        }
                        if (!string.IsNullOrEmpty(validationErrors))
                        {
                            throw new SevenZipSfxValidationException(
                                "\n" + validationErrors.Substring(0, validationErrors.Length - 1));
                        }
                        _sfxCommands.Add(SfxModule.Default, _sfxCommands[SfxModule.Extended]);
                    }
                }
            }
        }

        /// <summary>
        /// 验证 sfx 场景命令。
        /// </summary>
        /// <param name="settings">要验证的 sfx 设置字典。</param>
        private void ValidateSettings(SfxSettings settings)
        {
            if (SfxModule == SfxModule.Custom)
            {
                return;
            }

            var commands = _sfxCommands[SfxModule];
            
            if (commands == null)
            {
                return;
            }
            
            var invalidCommands = new List<string>();
            
            foreach (var command in settings.Keys)
            {
                if (!commands.Contains(command))
                {
                    invalidCommands.Add(command);
                }
            }
            
            if (invalidCommands.Count > 0)
            {
                var invalidText = new StringBuilder("\nInvalid commands:\n");
                
                foreach (var str in invalidCommands)
                {
                    invalidText.Append(str);
                }
                
                throw new SevenZipSfxValidationException(invalidText.ToString());
            }
        }

        /// <summary>
        /// 获取包含 sfx 设置的流。
        /// </summary>
        /// <param name="settings">sfx 设置字典。</param>
        /// <returns></returns>
        private static Stream GetSettingsStream(SfxSettings settings)
        {
            var ms = new MemoryStream();
            var buf = Encoding.UTF8.GetBytes(@";!@Install@!UTF-8!" + '\n');
            ms.Write(buf, 0, buf.Length);
            
            foreach (var command in settings.Keys)
            {
                buf =
                    Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, "{0}=\"{1}\"\n", command,
                                                         settings[command]));
                ms.Write(buf, 0, buf.Length);
            }
           
            buf = Encoding.UTF8.GetBytes(@";!@InstallEnd@!");
            ms.Write(buf, 0, buf.Length);
            
            return ms;
        }

        private SfxSettings GetDefaultSettings()
        {
            switch (SfxModule)
            {
                default:
                    return null;
                case SfxModule.Installer:
                    var settings = new Dictionary<string, string> {{"Title", "7-Zip self-extracting archive"}};
                    return settings;
                case SfxModule.Default:
                case SfxModule.Extended:
                    settings = new Dictionary<string, string>
                               {
                                   {"GUIMode", "0"},
                                   {"InstallPath", "."},
                                   {"GUIFlags", "128+8"},
                                   {"ExtractPathTitle", "7-Zip self-extracting archive"},
                                   {"ExtractPathText", "Specify the path where to extract the files:"}
                               };
                    return settings;
            }
        }

        /// <summary>
        /// 将整个流写入另一个流。
        /// </summary>
        /// <param name="src">要读取的源流。</param>
        /// <param name="dest">要写入的目标流。</param>
        private static void WriteStream(Stream src, Stream dest)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            src.Seek(0, SeekOrigin.Begin);
            var buf = new byte[32768];
            int bytesRead;
            
            while ((bytesRead = src.Read(buf, 0, buf.Length)) > 0)
            {
                dest.Write(buf, 0, bytesRead);
            }
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archive">归档流。</param>
        /// <param name="sfxFileName">自解压可执行文件的名称。</param>
        public void MakeSfx(Stream archive, string sfxFileName)
        {
            using (Stream sfxStream = new FileStream(sfxFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan))
            {
                MakeSfx(archive, GetDefaultSettings(), sfxStream);
            }
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archive">归档流。</param>
        /// <param name="sfxStream">用于写入自解压可执行文件的流。</param>
        public void MakeSfx(Stream archive, Stream sfxStream)
        {
            MakeSfx(archive, GetDefaultSettings(), sfxStream);
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archive">归档流。</param>
        /// <param name="settings">sfx 设置。</param>
        /// <param name="sfxFileName">自解压可执行文件的名称。</param>
        public void MakeSfx(Stream archive, SfxSettings settings, string sfxFileName)
        {
            using (Stream sfxStream = new FileStream(sfxFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan))
            {
                MakeSfx(archive, settings, sfxStream);
            }
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archive">归档流。</param>
        /// <param name="settings">sfx 设置。</param>
        /// <param name="sfxStream">用于写入自解压可执行文件的流。</param>
        public void MakeSfx(Stream archive, SfxSettings settings, Stream sfxStream)
        {
            if (!sfxStream.CanWrite)
            {
                throw new ArgumentException("The specified output stream can not write.", "sfxStream");
            }

            ValidateSettings(settings);

            using (var sfx = s_assembly.GetManifestResourceStream(GetResourceString(SfxSupportedModuleNames[SfxModule][0])))
            {
                WriteStream(sfx, sfxStream);
            }

            if (SfxModule == SfxModule.Custom || _sfxCommands[SfxModule] != null)
            {
                using (var set = GetSettingsStream(settings))
                {
                    WriteStream(set, sfxStream);
                }
            }

            WriteStream(archive, sfxStream);
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archiveFileName">归档文件名。</param>
        /// <param name="sfxFileName">自解压可执行文件的名称。</param>
        public void MakeSfx(string archiveFileName, string sfxFileName)
        {
            using (Stream sfxStream = new FileStream(sfxFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan))
            {
                using (
                    Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan)
                    )
                {
                    MakeSfx(archive, GetDefaultSettings(), sfxStream);
                }
            }
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archiveFileName">归档文件名。</param>
        /// <param name="sfxStream">用于写入自解压可执行文件的流。</param>
        public void MakeSfx(string archiveFileName, Stream sfxStream)
        {
            using (Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan)
                )
            {
                MakeSfx(archive, GetDefaultSettings(), sfxStream);
            }
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archiveFileName">归档文件名。</param>
        /// <param name="settings">sfx 设置。</param>
        /// <param name="sfxFileName">自解压可执行文件的名称。</param>
        public void MakeSfx(string archiveFileName, SfxSettings settings, string sfxFileName)
        {
            using (Stream sfxStream = new FileStream(sfxFileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan))
            {
                using (
                    Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan)
                    )
                {
                    MakeSfx(archive, settings, sfxStream);
                }
            }
        }

        /// <summary>
        /// 制作自解压归档。
        /// </summary>
        /// <param name="archiveFileName">归档文件名。</param>
        /// <param name="settings">sfx 设置。</param>
        /// <param name="sfxStream">用于写入自解压可执行文件的流。</param>
        public void MakeSfx(string archiveFileName, SfxSettings settings, Stream sfxStream)
        {
            using (Stream archive = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan)
                )
            {
                MakeSfx(archive, settings, sfxStream);
            }
        }
    }
#endif
}