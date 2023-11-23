// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FancyZonesEditorCommon.Data;
using Microsoft.FancyZonesEditor.UITests.Utils;
using Microsoft.FancyZonesEditor.UnitTests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static FancyZonesEditorCommon.Data.EditorParameters;

namespace Microsoft.FancyZonesEditor.UITests
{
    [TestClass]
    public class RunFancyZonesEditorTest
    {
        private static FancyZonesEditorSession? _session;
        private static IOTestHelper? _ioHelper;
        private static TestContext? _context;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            _context = testContext;

            // prepare test editor parameters with 2 monitors before launhing the editor
            EditorParameters editorParameters = new EditorParameters();
            _ioHelper = new IOTestHelper(editorParameters.File);
            ParamsWrapper parameters = new ParamsWrapper
            {
                ProcessId = 1,
                SpanZonesAcrossMonitors = false,
                Monitors = new List<NativeMonitorDataWrapper>
                {
                    new NativeMonitorDataWrapper
                    {
                        Monitor = "monitor-1",
                        MonitorInstanceId = "instance-id-1",
                        MonitorSerialNumber = "serial-number-1",
                        MonitorNumber = 1,
                        VirtualDesktop = "{FF34D993-73F3-4B8C-AA03-73730A01D6A8}",
                        Dpi = 96,
                        LeftCoordinate = 0,
                        TopCoordinate = 0,
                        WorkAreaHeight = 1040,
                        WorkAreaWidth = 1920,
                        MonitorHeight = 1080,
                        MonitorWidth = 1920,
                        IsSelected = true,
                    },
                    new NativeMonitorDataWrapper
                    {
                        Monitor = "monitor-2",
                        MonitorInstanceId = "instance-id-2",
                        MonitorSerialNumber = "serial-number-2",
                        MonitorNumber = 2,
                        VirtualDesktop = "{FF34D993-73F3-4B8C-AA03-73730A01D6A8}",
                        Dpi = 96,
                        LeftCoordinate = 1920,
                        TopCoordinate = 0,
                        WorkAreaHeight = 1040,
                        WorkAreaWidth = 1920,
                        MonitorHeight = 1080,
                        MonitorWidth = 1920,
                        IsSelected = false,
                    },
                },
            };
            _ioHelper.WriteData(editorParameters.Serialize(parameters));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _ioHelper?.RestoreData();
            _ioHelper = null;

            _context = null;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _session = new FancyZonesEditorSession(_context!);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _session?.Close(_context!);
        }

        [TestMethod]
        public void OpenEditorWindow() // verify the session is initialized
        {
            Assert.IsNotNull(_session?.Session);
        }

        [TestMethod]
        public void OpenNewLayoutDialog() // verify the new layout dialog is opened
        {
            _session?.Click_CreateNewLayout();
            Assert.IsNotNull(_session?.Session?.FindElementsByName("Choose layout type")); // check the pane header
        }

        [TestMethod]
        public void OpenEditLayoutDialog() // verify the edit layout dialog is opened
        {
            _session?.Click_EditLayout(Constants.TemplateLayoutNames[Constants.TemplateLayouts.Grid]);
            Assert.IsNotNull(_session?.Session?.FindElementByAccessibilityId("EditLayoutDialogTitle")); // check the pane header
            Assert.IsNotNull(_session?.Session?.FindElementsByName("Edit 'Grid'")); // verify it's opened for the correct layout
        }

        [TestMethod]
        public void OpenContextMenu() // verify the context menu is opened
        {
            Assert.IsNotNull(_session?.OpenContextMenu(Constants.TemplateLayoutNames[Constants.TemplateLayouts.Columns]));
        }

        [TestMethod]
        public void ClickMonitor()
        {
            Assert.IsNotNull(_session?.GetMonitorItem(1));
            Assert.IsNotNull(_session?.GetMonitorItem(2));

            // verify that the monitor 1 is selected initially
            Assert.IsTrue(_session?.GetMonitorItem(1)?.Selected);
            Assert.IsFalse(_session?.GetMonitorItem(2)?.Selected);

            _session?.Click_Monitor(2);

            // verify that the monitor 2 is selected after click
            Assert.IsFalse(_session?.GetMonitorItem(1)?.Selected);
            Assert.IsTrue(_session?.GetMonitorItem(2)?.Selected);
        }
    }
}
