@Echo off
dism /Online /Remove-ProvisionedAppxPackage /PackageName:Pearson.PSC_2014.1029.1827.1288_neutral_~_2dnf9zyx96z42
pause
powershell -Command Remove-AppxPackage Pearson.PSC_1.4.3.0_x86__2dnf9zyx96z42
pause