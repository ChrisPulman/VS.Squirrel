# Road map

- [x] A feature that has been completed
- [ ] A feature that has NOT yet been completed
---
- [x] Test with a single project
- [x] Add the ability to create an exe locally
- [x] Add the ability to Publish to an S3 storage account
- [x] Add the ability to Build Full Releases
- [x] Add the ability to Build Update Releases
- [x] Test with multiple projects
- [x] Gate the visibility of the toolwindow to a Squirrel Project selection only
- [x] Detect projects that have no exe in build folder
- [x] Add Settings
- [ ] Check that the project has been saved
- [ ] Check that the project has been built
---
Features that have a checkmark are complete and available for
download in the
[CI build](http://vsixgallery.com/extension/VS.Squirrel.Chris.Pulman.b619c884-a2aa-4750-8433-bdca671f6d26/).

# Change log

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

## 0.0.1

- [x] Create a UI for Visual Studio as a toolwindow
- [x] Add files from the build directory automatically when a project is selected
- [x] Filter out unwanted files
- [x] Detect the exe version, Author and Description
- [x] Releasify the files from this folder

## 0.0.2
- [x] Fix bug with spaces in the output path during releasify
- [x] Added automatic visualisation of the Squirrel Packager when project is selected
- [x] Execute on a background thread to allow VS to continue operating without locking

## 0.0.12
- [x] Show warnings for missing settings in project AssemblyInfo

## 2.0.2
- [x] Change package base to Clowd.Squirrel