import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Cabinet, Folder, Document, TreeNode } from '@/types'
import { cabinetsApi, foldersApi, documentsApi } from '@/api/client'

export const useDocumentsStore = defineStore('documents', () => {
  const cabinets = ref<Cabinet[]>([])
  const currentCabinet = ref<Cabinet | null>(null)
  const currentFolder = ref<Folder | null>(null)
  const folderPath = ref<Folder[]>([])
  const folders = ref<Folder[]>([])
  const subFolders = ref<Folder[]>([]) // Subfolders in the current view
  const documents = ref<Document[]>([])
  const treeNodes = ref<TreeNode[]>([])
  const loadedCabinetIds = ref<Set<string>>(new Set())
  const isLoading = ref(false)
  const isLoadingSubFolders = ref(false)
  const error = ref<string | null>(null)

  async function loadCabinets() {
    isLoading.value = true
    error.value = null
    try {
      const response = await cabinetsApi.getAll()
      cabinets.value = response.data
      loadedCabinetIds.value.clear()

      // Build tree nodes from cabinets
      treeNodes.value = cabinets.value.map(cab => ({
        id: cab.id,
        name: cab.name,
        type: 'cabinet' as const,
        children: [],
        isExpanded: false
      }))
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to load cabinets'
    } finally {
      isLoading.value = false
    }
  }

  async function loadFolderTree(cabinetId: string) {
    isLoading.value = true
    try {
      const response = await foldersApi.getTree(cabinetId)
      const folderTree = response.data as Folder[]

      // Update tree node for this cabinet
      const cabinetNode = treeNodes.value.find(n => n.id === cabinetId)
      if (cabinetNode) {
        cabinetNode.children = mapFoldersToTreeNodes(folderTree)
        cabinetNode.isExpanded = true
      }
      loadedCabinetIds.value.add(cabinetId)
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to load folders'
    } finally {
      isLoading.value = false
    }
  }

  function mapFoldersToTreeNodes(folders: Folder[]): TreeNode[] {
    return folders.map(folder => ({
      id: folder.id,
      name: folder.name,
      type: 'folder' as const,
      parentId: folder.parentFolderId,
      children: folder.children ? mapFoldersToTreeNodes(folder.children) : [],
      isExpanded: false,
      accessMode: folder.accessMode
    }))
  }

  async function loadDocuments(folderId: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await documentsApi.getByFolder(folderId)
      const data = response.data
      documents.value = Array.isArray(data) ? data : data.items ?? []
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to load documents'
    } finally {
      isLoading.value = false
    }
  }

  async function loadSubFolders(cabinetId: string, parentFolderId?: string) {
    isLoadingSubFolders.value = true
    try {
      const response = await foldersApi.getByParent(cabinetId, parentFolderId)
      const data = response.data
      subFolders.value = Array.isArray(data) ? data : data.items ?? []
    } catch (err: any) {
      subFolders.value = []
    } finally {
      isLoadingSubFolders.value = false
    }
  }

  async function selectCabinet(cabinet: Cabinet) {
    currentCabinet.value = cabinet
    currentFolder.value = null
    folderPath.value = []
    documents.value = []
  }

  function findNodeById(nodes: TreeNode[], id: string): TreeNode | null {
    for (const node of nodes) {
      if (node.id === id) return node
      if (node.children) {
        const found = findNodeById(node.children, id)
        if (found) return found
      }
    }
    return null
  }

  function updateNodeName(nodeId: string, newName: string) {
    // First check top-level nodes (cabinets)
    const cabinetNode = treeNodes.value.find(n => n.id === nodeId)
    if (cabinetNode) {
      cabinetNode.name = newName
      // Also update the cabinets array
      const cabinet = cabinets.value.find(c => c.id === nodeId)
      if (cabinet) cabinet.name = newName
      return true
    }
    // Search recursively for nested nodes (folders)
    const node = findNodeById(treeNodes.value, nodeId)
    if (node) {
      node.name = newName
      return true
    }
    return false
  }

  async function toggleCabinetExpansion(nodeId: string) {
    // First check if it's a top-level cabinet
    const cabinetNode = treeNodes.value.find(n => n.id === nodeId)
    if (cabinetNode) {
      if (cabinetNode.isExpanded) {
        // Collapse
        cabinetNode.isExpanded = false
      } else {
        // Expand - load children if not already loaded
        if (!loadedCabinetIds.value.has(nodeId)) {
          await loadFolderTree(nodeId)
        } else {
          cabinetNode.isExpanded = true
        }
      }
      return
    }

    // Otherwise, search recursively for nested folder node
    const node = findNodeById(treeNodes.value, nodeId)
    if (node) {
      node.isExpanded = !node.isExpanded
    }
  }

  async function selectFolder(folder: Folder) {
    // Build the folder path: if this folder's parent is in the current path, trim to it and add
    const parentIdx = folderPath.value.findIndex(f => f.id === folder.parentFolderId)
    if (parentIdx >= 0) {
      // Navigating into a child of a folder already in the path
      folderPath.value = [...folderPath.value.slice(0, parentIdx + 1), folder]
    } else if (folder.parentFolderId && currentFolder.value?.id === folder.parentFolderId) {
      // Going one level deeper from current folder
      folderPath.value = [...folderPath.value, folder]
    } else if (!folder.parentFolderId) {
      // Top-level folder in cabinet
      folderPath.value = [folder]
    } else {
      // Direct navigation (e.g. from URL) — just set the single folder
      folderPath.value = [folder]
    }
    currentFolder.value = folder
    await loadDocuments(folder.id)
  }

  function navigateToBreadcrumb(index: number) {
    // index -1 = cabinet root, 0+ = folder in path
    if (index < 0) {
      currentFolder.value = null
      folderPath.value = []
      documents.value = []
      return
    }
    const folder = folderPath.value[index]
    if (folder) {
      folderPath.value = folderPath.value.slice(0, index + 1)
      currentFolder.value = folder
      loadDocuments(folder.id)
    }
  }

  async function createCabinet(data: { name: string; description?: string }) {
    const response = await cabinetsApi.create(data)
    cabinets.value.push(response.data)
    treeNodes.value.push({
      id: response.data.id,
      name: response.data.name,
      type: 'cabinet',
      children: [],
      isExpanded: false
    })
    return response.data
  }

  async function createFolder(data: { cabinetId: string; parentFolderId?: string; name: string; description?: string }) {
    const response = await foldersApi.create(data)
    folders.value.push(response.data)
    return response.data
  }

  async function uploadDocument(folderId: string, file: File, metadata: { name: string; description?: string }) {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('folderId', folderId)
    formData.append('name', metadata.name)
    if (metadata.description) {
      formData.append('description', metadata.description)
    }

    const response = await documentsApi.upload(formData)
    documents.value.push(response.data)
    return response.data
  }

  async function deleteDocument(id: string) {
    await documentsApi.delete(id)
    documents.value = documents.value.filter(d => d.id !== id)
  }

  return {
    cabinets,
    currentCabinet,
    currentFolder,
    folderPath,
    folders,
    subFolders,
    documents,
    treeNodes,
    isLoading,
    isLoadingSubFolders,
    error,
    loadCabinets,
    loadFolderTree,
    loadDocuments,
    loadSubFolders,
    selectCabinet,
    selectFolder,
    navigateToBreadcrumb,
    toggleCabinetExpansion,
    createCabinet,
    createFolder,
    uploadDocument,
    deleteDocument,
    updateNodeName
  }
})
