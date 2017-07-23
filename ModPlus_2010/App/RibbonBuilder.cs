﻿#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System;
using System.Linq;
using System.Xml.Linq;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using RibbonPanelSource = Autodesk.Windows.RibbonPanelSource;
using RibbonRowPanel = Autodesk.Windows.RibbonRowPanel;
using RibbonSplitButton = Autodesk.Windows.RibbonSplitButton;
using RibbonSplitButtonListStyle = Autodesk.Windows.RibbonSplitButtonListStyle;

// ModPlus
using ModPlus.Helpers;
using ModPlusAPI;
using ModPlusAPI.Windows;
using Orientation = System.Windows.Controls.Orientation;

namespace ModPlus.App
{
    public static class RibbonBuilder
    {
        public static void BuildRibbon()
        {
            if (!IsLoaded())
            {
                CreateRibbon();
                AcApp.SystemVariableChanged += acadApp_SystemVariableChanged;
            }
        }
        private static bool IsLoaded()
        {
            var loaded = false;
            var ribCntrl = ComponentManager.Ribbon;
            foreach (var tab in ribCntrl.Tabs)
            {
                if (tab.Id.Equals("ModPlus_ID") & tab.Title.Equals("ModPlus"))
                    loaded = true;
                else loaded = false;
            }
            return loaded;
        }
        public static void RemoveRibbon()
        {
            try
            {
                if (IsLoaded())
                {
                    var ribCntrl = ComponentManager.Ribbon;
                    foreach (var tab in ribCntrl.Tabs.Where(
                        tab => tab.Id.Equals("ModPlus_ID") & tab.Title.Equals("ModPlus")))
                    {
                        ribCntrl.Tabs.Remove(tab);
                        AcApp.SystemVariableChanged -= acadApp_SystemVariableChanged;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.ShowForConfigurator(exception);
            }
        }
        static void acadApp_SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name.Equals("WSCURRENT")) BuildRibbon();
        }
        private static void CreateRibbon()
        {
            try
            {
                var ribCntrl = ComponentManager.Ribbon;
                // add the tab
                var ribTab = new RibbonTab { Title = "ModPlus", Id = "ModPlus_ID" };
                ribCntrl.Tabs.Add(ribTab);
                // add content
                AddPanels(ribTab);
                // add help panel
                AddHelpPanel(ribTab);
                ////////////////////////
                ribCntrl.UpdateLayout();
            }
            catch (Exception exception)
            {
                ExceptionBox.ShowForConfigurator(exception);
            }
        }

        private static void AddPanels(RibbonTab ribTab)
        {
            try
            {
                // Расположение файла конфигурации
                var confF = UserConfigFile.FullFileName;
                // Грузим
                var configFile = XElement.Load(confF);
                // Проверяем есть ли группа Config
                if (configFile.Element("Config") == null)
                {
                    ModPlusAPI.Windows.MessageBox.Show("Файл конфигурации поврежден! Невозможно построить ленту", MessageBoxIcon.Close);
                    return;
                }
                var element = configFile.Element("Config");
                // Проверяем есть ли подгруппа Functions
                if (element?.Element("Functions") == null)
                {
                    ModPlusAPI.Windows.MessageBox.Show("Файл конфигурации поврежден! Невозможно построить ленту", MessageBoxIcon.Close);
                    return;
                }
                var confCuiXel = element.Element("CUI");
                // Проходим по группам
                if (confCuiXel != null)
                    foreach (var group in confCuiXel.Elements("Group"))
                    {
                        if(group.Attribute("GroupName") == null) continue;
                        // create the panel source
                        var ribSourcePanel = new RibbonPanelSource
                        {
                            Title = group.Attribute("GroupName")?.Value
                        };
                        // now the panel
                        var ribPanel = new RibbonPanel
                        {
                            Source = ribSourcePanel
                        };
                        ribTab.Panels.Add(ribPanel);
                        var ribRowPanel = new RibbonRowPanel();
                        // Вводим спец.счетчик, который потребуется для разбивки по строкам
                        var nr = 0;
                        var hasFunctions = false;
                        // Если последняя функция в группе была 32х32
                        var lastWasBig = false;
                        // Проходим по функциям группы
                        foreach (var func in group.Elements("Function"))
                        {
                            var fNameAttr = func.Attribute("Name")?.Value;
                            if (string.IsNullOrEmpty(fNameAttr)) continue;
                            if (LoadFunctionsHelper.LoadedFunctions.Any(x => x.Name.Equals(fNameAttr)))
                            {
                                var loadedFunction = LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x => x.Name.Equals(fNameAttr));
                                if(loadedFunction == null) continue;
                                hasFunctions = true;
                                if (nr == 0) ribRowPanel = new RibbonRowPanel();
                                // В зависимости от размера
                                var btnSizeAttr = func.Attribute("WH")?.Value;
                                if (string.IsNullOrEmpty(btnSizeAttr)) continue;
                                #region 16
                                if (btnSizeAttr.Equals("16"))
                                {
                                    lastWasBig = false;
                                    // Если функция имеет "подфункции", то делаем SplitButton
                                    if (func.Elements("SubFunction").Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Horizontal,
                                            Size = RibbonItemSize.Standard,
                                            ShowImage = true,
                                            ShowText = false,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };
                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddButton(
                                            loadedFunction.Name, loadedFunction.LName, loadedFunction.SmallIconUrl,
                                            loadedFunction.BigIconUrl,
                                            loadedFunction.Description, Orientation.Horizontal,
                                            loadedFunction.FullDescription, loadedFunction.ToolTipHelpImage
                                        );
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;
                                        // Затем добавляем подфункции
                                        foreach (var subFunc in func.Elements("SubFunction"))
                                        {
                                            if (LoadFunctionsHelper.LoadedFunctions.Any(x => x.Name.Equals(subFunc.Attribute("Name")?.Value)))
                                            {
                                                var loadedSubFunction = LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x => x.Name.Equals(subFunc.Attribute("Name")?.Value));
                                                if(loadedSubFunction == null) continue;
                                                risSplitBtn.Items.Add(RibbonHelpers.AddButton(
                                                    loadedSubFunction.Name, loadedSubFunction.LName, loadedSubFunction.SmallIconUrl, loadedSubFunction.BigIconUrl,
                                                    loadedSubFunction.Description, Orientation.Horizontal, loadedSubFunction.FullDescription, loadedSubFunction.ToolTipHelpImage
                                                    ));
                                            }
                                        }
                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }
                                    // Если в конфигурации меню не прописано наличие подфункций, то проверяем, что они могут быть в самой функции
                                    else if (loadedFunction.SubFunctionsNames.Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Horizontal,
                                            Size = RibbonItemSize.Standard,
                                            ShowImage = true,
                                            ShowText = false,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };
                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddButton(
                                            loadedFunction.Name, loadedFunction.LName, loadedFunction.SmallIconUrl,
                                            loadedFunction.BigIconUrl,
                                            loadedFunction.Description, Orientation.Horizontal,
                                            loadedFunction.FullDescription, loadedFunction.ToolTipHelpImage
                                        );
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;
                                        // Затем добавляем подфункции
                                        for (int i = 0; i < loadedFunction.SubFunctionsNames.Count; i++)
                                        {
                                            risSplitBtn.Items.Add(RibbonHelpers.AddButton(
                                                loadedFunction.SubFunctionsNames[i], loadedFunction.SubFunctionsLNames[i],
                                                loadedFunction.SubSmallIconsUrl[i], loadedFunction.SubBigIconsUrl[i],
                                                loadedFunction.SubDescriptions[i], Orientation.Horizontal,
                                                loadedFunction.SubFullDescriptions[i], loadedFunction.SubHelpImages[i]
                                                ));
                                        }
                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }
                                    // Иначе просто добавляем маленькую кнопку
                                    else
                                    {
                                        ribRowPanel.Items.Add(RibbonHelpers.AddSmallButton(
                                            loadedFunction.Name, loadedFunction.LName, loadedFunction.SmallIconUrl,
                                            loadedFunction.Description, loadedFunction.FullDescription, loadedFunction.ToolTipHelpImage
                                            ));
                                    }
                                    nr++;
                                    if (nr == 3 | nr == 6) ribRowPanel.Items.Add(new RibbonRowBreak());
                                    if (nr == 9)
                                    {
                                        ribSourcePanel.Items.Add(ribRowPanel);
                                        nr = 0;
                                    }
                                }
                                #endregion
                                // Если кнопка большая, то добавляем ее в отдельную Row Panel
                                #region 32
                                if (btnSizeAttr.Equals("32"))
                                {
                                    lastWasBig = true;
                                    if (ribRowPanel.Items.Count > 0)
                                    {
                                        ribSourcePanel.Items.Add(ribRowPanel);
                                        nr = 0;
                                    }
                                    ribRowPanel = new RibbonRowPanel();
                                    // Если функция имеет "подфункции", то делаем SplitButton
                                    if (func.Elements("SubFunction").Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Vertical,
                                            Size = RibbonItemSize.Large,
                                            ShowImage = true,
                                            ShowText = true,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };
                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddBigButton(
                                            loadedFunction.Name, loadedFunction.LName, loadedFunction.BigIconUrl,
                                            loadedFunction.Description, Orientation.Horizontal,
                                            loadedFunction.FullDescription, loadedFunction.ToolTipHelpImage
                                        );
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;
                                        // Затем добавляем подфункции
                                        foreach (var subFunc in func.Elements("SubFunction"))
                                        {
                                            if (LoadFunctionsHelper.LoadedFunctions.Any(x => x.Name.Equals(subFunc.Attribute("Name")?.Value)))
                                            {
                                                var loadedSubFunction = LoadFunctionsHelper.LoadedFunctions.FirstOrDefault(x => x.Name.Equals(subFunc.Attribute("Name")?.Value));
                                                if(loadedSubFunction == null) continue;
                                                risSplitBtn.Items.Add(RibbonHelpers.AddBigButton(
                                                    loadedSubFunction.Name, loadedSubFunction.LName, loadedSubFunction.BigIconUrl,
                                                    loadedSubFunction.Description, Orientation.Horizontal, loadedSubFunction.FullDescription,
                                                    loadedSubFunction.ToolTipHelpImage
                                                ));
                                            }
                                        }
                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }
                                    // Если в конфигурации меню не прописано наличие подфункций, то проверяем, что они могут быть в самой функции
                                    else if (loadedFunction.SubFunctionsNames.Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Vertical,
                                            Size = RibbonItemSize.Large,
                                            ShowImage = true,
                                            ShowText = true,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };
                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddBigButton(
                                            loadedFunction.Name, loadedFunction.LName, loadedFunction.BigIconUrl,
                                            loadedFunction.Description, Orientation.Horizontal,
                                            loadedFunction.FullDescription, loadedFunction.ToolTipHelpImage
                                        );
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;
                                        // Затем добавляем подфункции
                                        // Затем добавляем подфункции
                                        for (int i = 0; i < loadedFunction.SubFunctionsNames.Count; i++)
                                        {
                                            risSplitBtn.Items.Add(RibbonHelpers.AddBigButton(
                                                loadedFunction.SubFunctionsNames[i], loadedFunction.SubFunctionsLNames[i],
                                                loadedFunction.SubBigIconsUrl[i],
                                                loadedFunction.SubDescriptions[i], Orientation.Horizontal,
                                                loadedFunction.SubFullDescriptions[i], loadedFunction.SubHelpImages[i]
                                            ));
                                        }
                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }
                                    // Иначе просто добавляем большую кнопку
                                    else
                                    {
                                        ribRowPanel.Items.Add(RibbonHelpers.AddBigButton(
                                            loadedFunction.Name, loadedFunction.LName,
                                            loadedFunction.BigIconUrl, loadedFunction.Description,
                                            Orientation.Vertical, loadedFunction.FullDescription, loadedFunction.ToolTipHelpImage
                                            ));
                                    }
                                    ribSourcePanel.Items.Add(ribRowPanel);
                                }
                                #endregion
                            }
                        }// foreach functions
                        if (ribRowPanel.Items.Any() & !lastWasBig)
                        {
                            ribSourcePanel.Items.Add(ribRowPanel);
                        }
                        //Если в группе нет функций(например отключены), то не добавляем эту группу
                        if (!hasFunctions)
                            ribTab.Panels.Remove(ribPanel);
                    }
            }
            catch (Exception exception) { ExceptionBox.ShowForConfigurator(exception); }
        }

        private static void AddHelpPanel(RibbonTab ribTab)
        {
            // create the panel source
            var ribSourcePanel = new RibbonPanelSource
            {
                Title = "ModPlus"
            };
            // now the panel
            var ribPanel = new RibbonPanel
            {
                Source = ribSourcePanel
            };
            ribTab.Panels.Add(ribPanel);

            var ribRowPanel = new RibbonRowPanel();

            ribRowPanel.Items.Add(
                RibbonHelpers.AddBigButton(
                "mpSettings",
                "Настройки",
                "pack://application:,,,/Modplus_" + MpVersionData.CurCadVers + ";component/Resources/HelpBt.png",
                "Настройки ModPlus", Orientation.Vertical,
                "Основные настройки плагина ModPlus - темы оформления, включение/отключение меню, настройки контекстных меню для мини-функций. Так же позволяет узнать Ваш регистрационный ключ, не запуская Конфигуратор",
                ""
                ));
            // 
            string icon;
            if (LoadFunctionsHelper.HasmpStampsFunction(out icon))
                ribRowPanel.Items.Add(
                    RibbonHelpers.AddSmallButton(
                        "mpStampFields",
                        "Поля",
                        icon,
                        "Редактор полей",
                        "Редактор полей, используемых при заполнении штампов",
                        ""
                        )
                    );


            ribSourcePanel.Items.Add(ribRowPanel);
        }
    }
}
