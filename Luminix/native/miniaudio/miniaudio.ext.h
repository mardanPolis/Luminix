#include "fetched/miniaudio.h"
 
#ifndef MINIAUDIO_EXT_H
#define MINIAUDIO_EXT_H

typedef struct
{
    ma_sound sound; // sound must be the first field of ma_audio_source since I pass the ma_audio_source pointer directly into ma functions.
    ma_audio_buffer_ref ref;
} ma_audio_source;


MA_API ma_result ma_create_engine(ma_engine** ppEngine)
{
    if (ppEngine == NULL) {
        return MA_INVALID_ARGS;
    }
 
    ma_engine* pEngine = (ma_engine*)malloc(sizeof(*pEngine));
    if (pEngine == NULL) {
        *ppEngine = NULL;
        return MA_OUT_OF_MEMORY;
    }
 
    ma_result result = ma_engine_init(NULL, pEngine);
    if (result != MA_SUCCESS) {
        free(pEngine);
        *ppEngine = NULL;
        return result;
    }
 
    *ppEngine = pEngine;
    return MA_SUCCESS;
}
 
MA_API void ma_dispose_engine(ma_engine* pEngine)
{
    if (pEngine == NULL) {
        return;
    }
 
    ma_engine_uninit(pEngine);
    free(pEngine);
}

// SOUND GROUPS

MA_API ma_result ma_create_sound_group(ma_engine* pEngine, ma_sound_group* pParentGroup, ma_sound_group** ppGroup)
{
    if (pEngine == NULL || ppGroup == NULL) {
        return MA_INVALID_ARGS;
    }

    ma_sound_group* pGroup = (ma_sound_group*)malloc(sizeof(ma_sound_group));
    if (pGroup == NULL) {
        *ppGroup = NULL;
        return MA_OUT_OF_MEMORY;
    }

    ma_result result = ma_sound_group_init(pEngine, 0, pParentGroup, pGroup);
    if (result != MA_SUCCESS) {
        free(pGroup);
        *ppGroup = NULL;
        return result;
    }

    *ppGroup = pGroup;
    return MA_SUCCESS;
}

MA_API ma_result ma_dispose_sound_group(ma_sound_group* pGroup)
{
    if (pGroup == NULL) {
        return MA_INVALID_ARGS;
    }

    ma_sound_group_uninit(pGroup);
    free(pGroup);
    return MA_SUCCESS;
}

MA_API void ma_set_sound_group_volume(ma_sound_group* pGroup, float volume)
{
    ma_sound_group_set_volume(pGroup, volume);
}

// CLIP FUNCTIONS

MA_API ma_result ma_create_audio_clip(const void* pPCM, ma_uint64 frameCount, ma_uint32 channels, ma_format format, ma_uint32 sampleRate, ma_audio_buffer** ppAudioClip)
{
    if (ppAudioClip == NULL || pPCM == NULL || frameCount == 0) {
        return MA_INVALID_ARGS;
    }
 
    ma_audio_buffer* pBuffer = (ma_audio_buffer*)malloc(sizeof(ma_audio_buffer));
    if (pBuffer == NULL) {
        return MA_OUT_OF_MEMORY;
    }
 
    ma_audio_buffer_config bufferConfig = ma_audio_buffer_config_init(format, channels, frameCount, pPCM, NULL);
    bufferConfig.sampleRate = sampleRate;
 
    ma_result result = ma_audio_buffer_init_copy(&bufferConfig, pBuffer);
    if (result != MA_SUCCESS) {
        free(pBuffer);
        return result;
    }
 
    *ppAudioClip = pBuffer;
    return MA_SUCCESS;
}
 
MA_API ma_result ma_dispose_audio_clip(ma_audio_buffer* pAudioClip)
{
    if (pAudioClip == NULL) {
        return MA_INVALID_ARGS;
    }
 
    ma_audio_buffer_uninit(pAudioClip);
    free(pAudioClip);
    return MA_SUCCESS;
}


// AUDIO SOURCE FUNCTIONS

MA_API ma_result ma_create_audio_source(ma_engine* pEngine, ma_audio_buffer* pAudioClip, ma_sound_group* pGroup, ma_audio_source** ppSource)
{
    if (pEngine == NULL || pAudioClip == NULL || ppSource == NULL) {
        return MA_INVALID_ARGS;
    }
 
    ma_audio_source* pSource = (ma_audio_source*)malloc(sizeof(ma_audio_source));
    if (pSource == NULL) {
        return MA_OUT_OF_MEMORY;
    }
 
    // Bind a ref to the clip's format/data with its own independent cursor.
    ma_result result = ma_audio_buffer_ref_init(
        pAudioClip->ref.format,
        pAudioClip->ref.channels,
        pAudioClip->ref.pData,
        pAudioClip->ref.sizeInFrames,
        &pSource->ref);
    if (result != MA_SUCCESS) {
        free(pSource);
        return result;
    }
 
    result = ma_sound_init_from_data_source(pEngine, (ma_data_source*)&pSource->ref, 0, pGroup, &pSource->sound);
    if (result != MA_SUCCESS) {
        ma_audio_buffer_ref_uninit(&pSource->ref);
        free(pSource);
        return result;
    }
 
    *ppSource = pSource;
    return MA_SUCCESS;
}
 
MA_API ma_result ma_dispose_audio_source(ma_audio_source* pSource)
{
    if (pSource == NULL) {
        return MA_INVALID_ARGS;
    }
 
    ma_sound_uninit(&pSource->sound);
    ma_audio_buffer_ref_uninit(&pSource->ref);
    free(pSource);
    return MA_SUCCESS;
}

MA_API void ma_audio_source_play(ma_audio_source* pSource) {
    if(pSource == 0)
        return;

    ma_sound_seek_to_second(&pSource->sound, 0);
    ma_sound_start(&pSource->sound);
}

MA_API void ma_audio_source_stop(ma_audio_source* pSource) {
    if(pSource == 0)
        return;
        
    ma_sound_stop(&pSource->sound);
    ma_sound_seek_to_second(&pSource->sound, 0);
}

MA_API void ma_audio_source_resume(ma_audio_source* pSource) {
    ma_sound_start(&pSource->sound);
}

MA_API void ma_audio_source_pause(ma_audio_source* pSource) {
    ma_sound_stop(&pSource->sound);
}

#endif 