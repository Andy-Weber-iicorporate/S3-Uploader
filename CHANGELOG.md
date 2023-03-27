# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2023-3-27
- added android build support
- upgraded to 2021
- 
## [1.6.0] - 2022-2-18
- added a button to check for files changed within last x hours
- 
## [1.5.3] - 2022-1-05
- added a check to preserve the required file

## [1.5.2] - 2021-12-28
- fixed issue with uploading not completing in upload-log-previous was missing
- changed idle color from black to default color

## [1.5.1] - 2021-12-28
- fixed issue with upload-log not uploading the full file

## [1.5.0] - 2021-12-27
- changed log-file name to upload-log
- added an upload-log-previous
- added uploading of upload-log and upload-log-previous to S3
- adjusted window width to show whole word after progress bar
- color coated the status of the upload

## [1.4.4] - 2021-12-27
- added log file creation during upload process

## [1.4.3] - 2021-12-27
- iterated version in package.json to 1.4.3
- iterated to wrong version in previous push

## [1.4.2] - 2021-12-27
- iterated version in package.json to 1.4.2

## [1.4.1] - 2021-12-27
- removed check for "no files to upload". Previously if the only files found to upload were the catalogs it would not upload because no assets changed, but its possible a key in the catalog can checnage without assets changing so this check was removed.

## [1.4.0] - 2021-11-18
- re-added lock file for unity client. Unity client now reads from the direct bucket instead of the CDN so doesnt need invalidating

## [1.3.1] - 2021-11-17
- removed the deleting of not used client-lock file (see previous changelogs)

## [1.3.0] - 2021-11-17
- added ability to invalidate single objects instead of whole distribution (note this seems to take the same time as doing the whole distribution)
- removed client-lock and invalidation. invalidation takes longer than the copy

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
