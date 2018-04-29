#!/bin/bash
xgettext -D . -o raidbot.pot -p i18n -L 'C#' --from-code='UTF-8' `find . -name '*.cs' -print` 
