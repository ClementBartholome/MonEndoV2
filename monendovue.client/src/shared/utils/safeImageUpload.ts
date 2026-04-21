const HEIC_EXTENSIONS = ['.heic', '.heif']
const HEIC_MIME_MARKERS = ['image/heic', 'image/heif']

export interface PreparePhotoResult {
  file: File
  convertedFromHeic: boolean
  compressed: boolean
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

const compressImageIfNeeded = async (file: File): Promise<File> => {
  const isCompressibleType = ['image/jpeg', 'image/png', 'image/webp'].includes(file.type)
  if (!isCompressibleType || file.size <= 2 * 1024 * 1024) {
    return file
  }

  const image = await createImageFromFile(file)
  const maxDimension = 1600
  const ratio = Math.min(maxDimension / image.width, maxDimension / image.height, 1)
  const width = Math.round(image.width * ratio)
  const height = Math.round(image.height * ratio)

  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height

  const context = canvas.getContext('2d')
  if (!context) {
    return file
  }

  context.drawImage(image, 0, 0, width, height)

  const jpegBlob = await canvasToJpegBlob(canvas, 0.82)
  return new File([jpegBlob], `${getBaseName(file.name)}.jpg`, {
    type: 'image/jpeg',
    lastModified: Date.now(),
  })
}

const convertHeicToJpeg = async (file: File): Promise<File | null> => {
  try {
    const heic2anyModule = await import('heic2any')
    const heic2any = heic2anyModule.default

    const outputBlob = await heic2any({
      blob: file,
      toType: 'image/jpeg',
      quality: 0.9,
    })

    const normalizedBlob = Array.isArray(outputBlob) ? outputBlob[0] : outputBlob
    if (!(normalizedBlob instanceof Blob)) {
      return null
    }

    return new File([normalizedBlob], `${getBaseName(file.name)}.jpg`, {
      type: 'image/jpeg',
      lastModified: Date.now(),
    })
  } catch {
    return null
  }
}

export const preparePhotoForUpload = async (file: File): Promise<PreparePhotoResult> => {
  let currentFile = file
  let convertedFromHeic = false

  if (isHeicLike(file)) {
    const convertedFile = await convertHeicToJpeg(file)
    if (convertedFile) {
      currentFile = convertedFile
      convertedFromHeic = true
    }
  }

  const compressedFile = await compressImageIfNeeded(currentFile)

  return {
    file: compressedFile,
    convertedFromHeic,
    compressed: compressedFile !== currentFile,
  }
}

