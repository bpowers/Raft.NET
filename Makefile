# Copyright 2017 Bobby Powers. All rights reserved.
# Use of this source code is governed by the ISC
# license that can be found in the LICENSE file.

# quiet output, but allow us to look at what commands are being
# executed by passing 'V=1' to make, without requiring temporarily
# editing the Makefile.
ifneq ($V, 1)
MAKEFLAGS += -s
endif

# GNU Make stats a ton of unnecessary stuff by default; disable that.
.SUFFIXES:
%: %,v
%: RCS/%,v
%: RCS/%
%: s.%
%: SCCS/s.%
.SUFFIXES: .cs

all: build

build:
	dotnet run

clean:
	dotnet clean

.PHONY: all build clean
