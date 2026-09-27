const HEIC_EXTENSIONS = ['.heic', '.heif']
const HEIC_MIME_MARKERS = ['image/heic', 'image/heif']

export interface PreparePhotoResult {
  file: File
  convertedFromHeic: boolean
  compressed: boolean
}

export interface PreparePhotoOptions {
  targetMaxBytes?: number
}

const getExtension = (fileName: string): string => {
  const index = fileName.lastIndexOf('.')
  if (index < 0) return ''
  return fileName.substring(index).toLowerCase()
}

const getBaseName = (fileName: string): string => {
  const index = fileName.lastIndexOf('.')
  if (index < 0) return fileName || `photo-${Date.now()}`
  return fileName.substring(0, index) || `photo-${Date.now()}`
}

const isHeicLike = (file: File): boolean => {
  const extension = getExtension(file.name)
  if (HEIC_EXTENSIONS.includes(extension)) {
    return true
  }

  const mimeType = file.type.trim().toLowerCase()
  return HEIC_MIME_MARKERS.some((marker) => mimeType.includes(marker))
}

const createImageFromFile = (file: File): Promise<HTMLImageElement> => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()

    reader.onload = () => {
      const image = new Image()
      image.onload = () => resolve(image)
      image.onerror = () => reject(new Error('Impossible de lire l\'image.'))
      image.src = reader.result as string
    }

    reader.onerror = () => reject(new Error('Impossible de lire le fichier image.'))
    reader.readAsDataURL(file)
  })
}

const canvasToJpegBlob = (canvas: HTMLCanvasElement, quality: number): Promise<Blob> => {
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (!blob) {
        reject(new Error('Conversion image échouée.'))
        return
      }

      resolve(blob)
    }, 'image/jpeg', quality)
  })
}

const compressImageToTarget = async (file: File, targetMaxBytes: number): Promise<File> => {
  if (!file.type.startsWith('image/')) {
    return file
  }

  if (file.size <= targetMaxBytes) {
    return file
  }

  const image = await createImageFromFile(file)
  const qualitySteps = [0.86, 0.78, 0.7, 0.62, 0.55]
  const maxDimensions = [1600, 1280, 1024, 896, 768]
  let smallestBlob: Blob | null = null

  for (const maxDimension of maxDimensions) {
    const ratio = Math.min(maxDimension / image.width, maxDimension / image.height, 1)
    const width = Math.max(1, Math.round(image.width * ratio))
    const height = Math.max(1, Math.round(image.height * ratio))

    const canvas = document.createElement('canvas')
    canvas.width = width
    canvas.height = height

    const context = canvas.getContext('2d')
    if (!context) {
      continue
    }

    context.drawImage(image, 0, 0, width, height)

    for (const quality of qualitySteps) {
      const jpegBlob = await canvasToJpegBlob(canvas, quality)

      if (!smallestBlob || jpegBlob.size < smallestBlob.size) {
        smallestBlob = jpegBlob
      }

      if (jpegBlob.size <= targetMaxBytes) {
        return new File([jpegBlob], `${getBaseName(file.name)}.jpg`, {
          type: 'image/jpeg',
          lastModified: Date.now(),
        })
      }
    }
  }

  if (smallestBlob) {
    return new File([smallestBlob], `${getBaseName(file.name)}.jpg`, {
      type: 'image/jpeg',
      lastModified: Date.now(),
    })
  }

  return file
}

/**
 * Conversion par le navigateur lui-même : Safari (iOS 17+, macOS 14+) décode le HEIC nativement.
 * Pas de bibliothèque de décodage (heic2any) : elle lance un worker depuis une URL blob: et évalue du code,
 * ce que la Content-Security-Policy interdit. Ailleurs, la photo HEIC est envoyée telle quelle (acceptée par le serveur).
 */
const convertHeicToJpeg = async (file: File): Promise<File | null> => {
  try {
    const image = await createImageFromFile(file)
    const canvas = document.createElement('canvas')
    canvas.width = image.naturalWidth
    canvas.height = image.naturalHeight

    const context = canvas.getContext('2d')
    if (!context) {
      return null
    }

    context.drawImage(image, 0, 0)
    const jpegBlob = await canvasToJpegBlob(canvas, 0.9)

    return new File([jpegBlob], `${getBaseName(file.name)}.jpg`, {
      type: 'image/jpeg',
      lastModified: Date.now(),
    })
  } catch {
    return null
  }
}

export const preparePhotoForUpload = async (
  file: File,
  options: PreparePhotoOptions = {}
): Promise<PreparePhotoResult> => {
  const targetMaxBytes = options.targetMaxBytes ?? 900 * 1024
  let currentFile = file
  let convertedFromHeic = false

  if (isHeicLike(file)) {
    const convertedFile = await convertHeicToJpeg(file)
    if (convertedFile) {
      currentFile = convertedFile
      convertedFromHeic = true
    }
  }

  const compressedFile = await compressImageToTarget(currentFile, targetMaxBytes)

  return {
    file: compressedFile,
    convertedFromHeic,
    compressed: compressedFile !== currentFile,
  }
}

