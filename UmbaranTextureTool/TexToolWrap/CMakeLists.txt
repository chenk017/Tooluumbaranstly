cmake_minimum_required(VERSION 3.10)

project(TexToolWrap)

add_library(
    textoolwrap
    SHARED
    textoolwrap.cpp
)


target_include_directories(
    textoolwrap
    PRIVATE
    .
    PVRTexLib/Include
    ispc/include
    crunch/inc
)


set_target_properties(
    textoolwrap
    PROPERTIES
    OUTPUT_NAME "textoolwrap"
)
