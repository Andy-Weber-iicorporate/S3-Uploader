# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [1.2.3] - 2021-11-17
- added invalidating after creating lock file

## [1.2.2] - 2021-11-16
- fixed bug with progress window trying to display lock file second time

## [1.2.1] - 2021-11-16
- fixed bug with progress window trying to display lock file
- removed debug log for percentage updates on file uploading

## [1.2.0] - 2021-11-16
- added lock file creation for the player client to check for when loading catalogs
- added deleting previous backup directory on s3 before creating a new one

## [1.1.0] - 2021-11-16
- added functionality for creating a backup of the s3 directory you are about to adjust

## [1.0.4] - 2021-11-16
- changed default key for bucket

## [1.0.3] - 2021-11-16
- refactored script

## [1.0.2] - 2021-11-16
- fixed complete button showing up too soon
- added changelog

## [1.0.1] - 2021-11-16
- removed extra "uploader" button

## [1.0.0] - 2021-11-16
- Initial submission for package distribution
