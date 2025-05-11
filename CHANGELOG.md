# Changelog

All notable changes to the Scene Compass package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2024-03-19

### Added
- Bookmark System
  - Smooth camera lerping when navigating to a camera view bookmark

### Fixed
- Measure Tool
  - Fixed M key detection to reliably show tooltip while holding the key
  - Fixed Control/Command key detection for grid snapping
  - Improved measurement point placement accuracy
- Bookmark System
  - Fixed camera view bookmarks to maintain exact position and rotation
  - Improved camera view saving by using SceneView pivot/rotation instead of camera transform

## [1.0.0] - 2024-03-19

### Added
- Initial release of Scene Compass
- Measure Tool
  - Hold M key to measure distances in the scene
  - Click to place measurement points
  - Hold Control/Command to snap to grid
  - Right-click to clear measurements
  - Real-time distance preview
  - Total path distance calculation
- Bookmark System
  - Save and organize camera views and GameObject positions
  - Group bookmarks for better organization
  - Cross-scene bookmark support
  - Search functionality
  - Context menus for quick actions
  - Keyboard shortcuts for efficient workflow
  - Modern UI with banner and footer
  - About and Help windows 