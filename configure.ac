dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([HyperFastCgi], [0.4])
AC_CANONICAL_SYSTEM
AC_CONFIG_MACRO_DIR([build/m4])
AM_INIT_AUTOMAKE([silent-rules foreign])
AM_MAINTAINER_MODE
AM_SILENT_RULES([yes])

AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

# Checks for programs.
AC_PROG_INSTALL
AC_PROG_CC([gcc])
AC_PROG_MKDIR_P
AM_PROG_LIBTOOL

LT_INIT

# Checks for required packages
PKG_CHECK_MODULES([GLIB],[glib-2.0 >= 2.20])
PKG_CHECK_MODULES([LIBEVENT],[libevent >= 2.0])
PKG_CHECK_MODULES([LIBEVENT_PTHREADS],[libevent_pthreads >= 2.0])


# Mono
SHAMROCK_CHECK_MONO2_MODULE(2.10.0)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_4_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_FIND_PROGRAM_OR_BAIL(GACUTIL, gacutil)
SHAMROCK_FIND_PROGRAM_OR_BAIL(SN, sn)

SAVED_DMCS="$DMCS"
SAVED_GMCS="$GMCS"

if test "x$RUNTIME" = "x" ; then
   RUNTIME="$MONO"
fi

CS="C#"
if test "x$GMCS" = "xno" -a "x$DMCS" = "xno" ; then
AC_MSG_ERROR([No $CS compiler found. gmcs or dmcs must be installed.])
fi

GACUTIL_FLAGS='-root $(DESTDIR)$(prefix)/lib'

case "$host" in
*-*-mingw*|*-*-cygwin*)
platform_win32=yes
;;
esac

#AM_CONDITIONAL(UNITTESTS, test x${UNIT_TESTS} = xyes)
#AM_CONDITIONAL(PLATFORM_WIN32, test x$platform_win32 = xyes)
AM_CONDITIONAL(NET_2_0, test ! x$GMCS = xno)
AM_CONDITIONAL(NET_4_0, test ! x$DMCS = xno)

AC_SUBST(GMCS)
AC_SUBST(DMCS)
AC_SUBST(RUNTIME)
AC_SUBST(GACUTIL)
AC_SUBST(GACUTIL_FLAGS)
AC_SUBST(SN)

AC_ARG_ENABLE(debug_x86,
	AC_HELP_STRING([--enable-debug_x86],
		[Use 'DEBUG_X86' Configuration [default=NO]]),
		enable_debug_x86=yes, enable_debug_x86=no)
AM_CONDITIONAL(ENABLE_DEBUG_X86, test x$enable_debug_x86 = xyes)
if test "x$enable_debug_x86" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release_x86,
	AC_HELP_STRING([--enable-release_x86],
		[Use 'RELEASE_X86' Configuration [default=YES]]),
		enable_release_x86=yes, enable_release_x86=no)
AM_CONDITIONAL(ENABLE_RELEASE_X86, test x$enable_release_x86 = xyes)
if test "x$enable_release_x86" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE_X86, true)
	enable_release_x86=yes
fi


dnl package checks, common for all configs
#AM_COND_IF([NET_4_0],[PKG_CHECK_MODULES([XSP_4],[xsp-4])])
#AM_COND_IF([NET_2_0],[PKG_CHECK_MODULES([XSP_2],[xsp-2])])


AC_CONFIG_FILES([Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool
src/Makefile
src/HyperFastCgi/Makefile
src/HyperFastCgi/scripts/Makefile
src/libnative/Makefile
])

AM_COND_IF([NET_4_0],[AC_CONFIG_FILES([src/HyperFastCgi/scripts/hyperfastcgi4])])
AM_COND_IF([NET_2_0],[AC_CONFIG_FILES([src/HyperFastCgi/scripts/hyperfastcgi2])])


#SHAVE_INIT([build/m4/shave], [enable])
AC_OUTPUT

TARGET_FRAMEWORKS=""
if test "x$SAVED_GMCS" = "xno"; then
   GMCS_STRING="not found"
else
   GMCS_STRING="$SAVED_GMCS"
   TARGET_FRAMEWORKS=".NET 2.0"
fi

if test "x$SAVED_DMCS" = "xno"; then
   DMCS_STRING="not found"
else
   DMCS_STRING="$SAVED_DMCS"
   if test -n "$TARGET_FRAMEWORKS"; then
      TARGET_FRAMEWORKS="$TARGET_FRAMEWORKS, .NET 4.0"
   else
      TARGET_FRAMEWORKS=".NET 4.0"
   fi
fi

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install prefix:          ${prefix}
    Datadir:                 ${expanded_datadir}
    Libdir:                  ${expanded_libdir}
    Build documentation:     ${enable_docs}
    Mono 2.0 compiler:       ${GMCS_STRING}
    Mono 4.0 compiler:       ${DMCS_STRING}
    Target frameworks:       ${TARGET_FRAMEWORKS}
EOF
