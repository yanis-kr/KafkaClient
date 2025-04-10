# PowerShell script to run tests with coverage collection and generate a report

# Configuration
$testProjectPath = "tests/KafkaConsumer.Tests"
$coverageOutputPath = "coverage"
$reportOutputPath = "coverage-report"

# Create output directories if they don't exist
if (-not (Test-Path $coverageOutputPath)) {
    New-Item -ItemType Directory -Path $coverageOutputPath | Out-Null
}
if (-not (Test-Path $reportOutputPath)) {
    New-Item -ItemType Directory -Path $reportOutputPath | Out-Null
}

# Run tests with coverage collection
Write-Host "Running tests with coverage collection..."
dotnet test $testProjectPath --collect:"XPlat Code Coverage" --results-directory $coverageOutputPath

# Check if tests were successful
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed. Exiting..." -ForegroundColor Red
    exit 1
}

# Generate coverage report
Write-Host "Generating coverage report..."
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"$coverageOutputPath/**/coverage.cobertura.xml" -targetdir:"$reportOutputPath" -reporttypes:"Html;HtmlSummary"

# Open the report in Internet Explorer
Write-Host "Opening coverage report..."
$reportPath = Join-Path $PWD $reportOutputPath "index.html"
Start-Process "iexplore.exe" -ArgumentList $reportPath

Write-Host "Done!" -ForegroundColor Green 