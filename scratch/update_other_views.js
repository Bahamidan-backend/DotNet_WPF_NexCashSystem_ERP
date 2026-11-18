const fs = require('fs');

// 1. MonthlyReportsView.xaml.cs
let mrCode = fs.readFileSync('POSSystem.WPF/Views/AdminPages/MonthlyReportsView.xaml.cs', 'utf8');
const mrTarget = `DataContext = _viewModel;
        }`;
const mrRepl = `DataContext = _viewModel;
            Loaded += (s, e) => { _viewModel?.GenerateReportCommand?.Execute(null); };
        }`;
if (mrCode.includes(mrTarget.replace(/\n/g, '\r\n'))) {
    mrCode = mrCode.replace(mrTarget.replace(/\n/g, '\r\n'), mrRepl.replace(/\n/g, '\r\n'));
    fs.writeFileSync('POSSystem.WPF/Views/AdminPages/MonthlyReportsView.xaml.cs', mrCode, 'utf8');
    console.log("Updated MonthlyReportsView.xaml.cs");
} else if (mrCode.includes(mrTarget)) {
    mrCode = mrCode.replace(mrTarget, mrRepl);
    fs.writeFileSync('POSSystem.WPF/Views/AdminPages/MonthlyReportsView.xaml.cs', mrCode, 'utf8');
    console.log("Updated MonthlyReportsView.xaml.cs");
}

// 2. CashierManagementView.xaml.cs
let cmCode = fs.readFileSync('POSSystem.WPF/Views/AdminPages/CashierManagementView.xaml.cs', 'utf8');
const cmTarget = `DataContext = _viewModel;
        }`;
const cmRepl = `DataContext = _viewModel;
            Loaded += async (s, e) => {
                if (_viewModel != null) {
                    await _viewModel.LoadCashierSalesReportAsync();
                }
            };
        }`;
if (cmCode.includes(cmTarget.replace(/\n/g, '\r\n'))) {
    cmCode = cmCode.replace(cmTarget.replace(/\n/g, '\r\n'), cmRepl.replace(/\n/g, '\r\n'));
    fs.writeFileSync('POSSystem.WPF/Views/AdminPages/CashierManagementView.xaml.cs', cmCode, 'utf8');
    console.log("Updated CashierManagementView.xaml.cs");
} else if (cmCode.includes(cmTarget)) {
    cmCode = cmCode.replace(cmTarget, cmRepl);
    fs.writeFileSync('POSSystem.WPF/Views/AdminPages/CashierManagementView.xaml.cs', cmCode, 'utf8');
    console.log("Updated CashierManagementView.xaml.cs");
}

