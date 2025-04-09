# PowerShell script to run tests with coverage and generate report

# Ensure ReportGenerator is installed
Write-Host "Installing/Checking ReportGenerator..." -ForegroundColor Green
dotnet tool install -g dotnet-reportgenerator-globaltool

# Install Coverlet collector
Write-Host "Installing Coverlet collector..." -ForegroundColor Green
dotnet add package coverlet.collector

# Create reports directory if it doesn't exist
$reportsDir = ".\reports"
if (-not (Test-Path $reportsDir)) {
    Write-Host "Creating reports directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $reportsDir
}

# Run tests with coverage collection
Write-Host "Running tests with coverage collection..." -ForegroundColor Green
$testResult = dotnet test `
    --collect:"XPlat Code Coverage" `
    --results-directory:".\reports" `
    /p:CoverletOutputFormat=cobertura `
    /p:Include="[KafkaConsumer*]*" `
    /p:Exclude="[*.Tests]*" `
    /p:SingleHit=false `
    /p:UseSourceLink=true `
    /p:IncludeTestAssembly=false

Write-Host "Test execution output:" -ForegroundColor Yellow
Write-Host $testResult

if ($LASTEXITCODE -ne 0) {
    Write-Host "Test execution failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Find the generated coverage file
$coverageFile = Get-ChildItem -Path ".\reports" -Recurse -Filter "*.cobertura.xml" | Select-Object -First 1
if (-not $coverageFile) {
    Write-Host "Coverage file was not generated in reports directory" -ForegroundColor Red
    Write-Host "Contents of reports directory:" -ForegroundColor Yellow
    Get-ChildItem -Path ".\reports" -Recurse | ForEach-Object { Write-Host $_.FullName }
    exit 1
}

Write-Host "Coverage file generated successfully at: $($coverageFile.FullName)" -ForegroundColor Green

# Generate HTML report
Write-Host "Generating coverage report..." -ForegroundColor Green
$reportResult = reportgenerator `
    -reports:"$($coverageFile.FullName)" `
    -targetdir:".\reports\html" `
    -reporttypes:"Html;HtmlSummary"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Report generation failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Verify report was generated
$reportIndex = ".\reports\html\index.htm"
if (-not (Test-Path $reportIndex)) {
    Write-Host "Report was not generated at: $reportIndex" -ForegroundColor Red
    exit 1
}

Write-Host "Report generated successfully at: $reportIndex" -ForegroundColor Green

# Open the report in Microsoft Edge using absolute path
Write-Host "Opening coverage report in Microsoft Edge..." -ForegroundColor Green
$reportPath = Join-Path $PWD.Path $reportIndex
Start-Process "msedge.exe" -ArgumentList "file:///$($reportPath.Replace('\', '/'))"

Write-Host "Done!" -ForegroundColor Green 