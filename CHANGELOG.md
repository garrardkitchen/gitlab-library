# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial creation of the changelog file.

## [2025-03-26]

####
- Updated version to 0.0.18 and added new feat to look for a placeholder in a file and replace it's value

## [2025-03-19]

####
- Updated version to 0.0.17 and added new feat to get a GitLab Group variable and create/update a Group Variable

## [2025-03-18]

#### Fixed
- Updated version to 0.0.16 and enhance project name existence check logic.

## [2025-03-07]

#### Added
- Updated version to 0.0.15 and can transfer project to another group or namespace 

## [2025-03-06]

### Added
- Updated version to 0.0.14 and enhanced error handling for unauthorized access with Personal Access Token
- Added functionality to create a file with content, and pushed initial commit, incremented version to 0.0.13
- Updated GitLab project creation to return project details such as Id and HttpUrlToRepo, and incremented version to 0.0.12
- Updated Git operations to support Personal Access Token (PAT) for repository access and incremented version to 0.0.11

### Documentation
- Updated CI and publish workflows to ignore changes in README.md and enhanced documentation
- Enhanced Git operations to support Personal Access Token (PAT) in DownloadGitRepository and CloneGitLabProject methods

## [2025-03-02]

### Added
- Updated version to 0.0.10 and fixed typos in README and code comments
- Updated requirements to reflect implemented actions and solution structure for GitLab project management
- Updated version to 0.0.9 and enhanced CreateGitLabProject to support optional group ID
- Updated version to 0.0.8 and enhanced DownloadGitRepository to support optional branch name
- Refactored Git operations to support branch creation and updated version to 0.0.7
- Enhanced project creation logic to handle existing project names and updated version to 0.0.6
- Updated installation command and example usage in README for version 0.0.5
- Updated CI workflow to trigger on feature branches instead of excluding main branch
- Bumped version to 0.0.4 and updated installation command in README
- Updated CI workflow to exclude main branch from push events
- Updated project name and description in README and .csproj for clarity
- Specified PowerShell syntax for installation command in README
- Updated README to reflect package name change from GitToolLibrary to Garrard.GitLab
- Added CI and publish workflows, restructured project files, and implemented file operations for GitLab integration
- Updated README and GitToolLibrary project version to 0.0.3 with new async methods and improved packaging
- Updated GitToolLibrary project version to 0.0.2 and enhanced packaging configuration
- Updated GitToolLibrary project configuration for NuGet publishing and changed package ID
- Updated project metadata in GitToolLibrary.csproj for versioning and description
- Updated README to clarify project structure and library usage
- Added Library with NuGet publishing workflow and updated project metadata
- Added CSharpFunctionalExtensions package and refactored GitToolApi methods for improved error handling
- Implemented user secrets configuration and enhanced project setup workflow
- Added Library for GitLab project management and repository operations
- Created a console app to create GL Project, cloned from a different repo to have this code committed and pushed to the new GL Project
