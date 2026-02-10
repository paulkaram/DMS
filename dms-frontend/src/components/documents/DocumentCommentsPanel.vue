<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { documentCommentsApi } from '@/api/client'
import { useAuthStore } from '@/stores/auth'

interface Comment {
  id: string
  documentId: string
  parentCommentId?: string
  content: string
  createdBy: string
  createdByName?: string
  createdAt: string
  modifiedAt?: string
  replyCount: number
  replies?: Comment[]
}

const props = defineProps<{
  documentId: string
  embedded?: boolean
}>()

const emit = defineEmits<{
  close: []
}>()

const authStore = useAuthStore()
const comments = ref<Comment[]>([])
const isLoading = ref(true)
const newComment = ref('')
const replyingTo = ref<string | null>(null)
const replyContent = ref('')
const editingComment = ref<string | null>(null)
const editContent = ref('')

onMounted(async () => {
  await loadComments()
})

async function loadComments() {
  isLoading.value = true
  try {
    const response = await documentCommentsApi.getByDocument(props.documentId)
    comments.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

async function addComment() {
  if (!newComment.value.trim()) return

  try {
    await documentCommentsApi.create(props.documentId, { content: newComment.value })
    newComment.value = ''
    await loadComments()
  } catch (error) {
  }
}

async function addReply(parentId: string) {
  if (!replyContent.value.trim()) return

  try {
    await documentCommentsApi.create(props.documentId, {
      content: replyContent.value,
      parentCommentId: parentId
    })
    replyingTo.value = null
    replyContent.value = ''
    await loadComments()
  } catch (error) {
  }
}

async function updateComment(commentId: string) {
  if (!editContent.value.trim()) return

  try {
    await documentCommentsApi.update(props.documentId, commentId, { content: editContent.value })
    editingComment.value = null
    editContent.value = ''
    await loadComments()
  } catch (error) {
  }
}

async function deleteComment(commentId: string) {
  if (!confirm('Are you sure you want to delete this comment?')) return

  try {
    await documentCommentsApi.delete(props.documentId, commentId)
    await loadComments()
  } catch (error) {
  }
}

async function loadReplies(commentId: string) {
  try {
    const response = await documentCommentsApi.getReplies(props.documentId, commentId)
    const comment = comments.value.find(c => c.id === commentId)
    if (comment) {
      comment.replies = response.data
    }
  } catch (error) {
  }
}

function startEdit(comment: Comment) {
  editingComment.value = comment.id
  editContent.value = comment.content
}

function cancelEdit() {
  editingComment.value = null
  editContent.value = ''
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString()
}

function isOwner(comment: Comment) {
  return comment.createdBy === authStore.user?.id
}
</script>

<template>
  <div :class="embedded ? '-mx-6 -my-5' : 'flex flex-col h-full bg-white dark:bg-background-dark'">
    <!-- Header (hidden when embedded in UiModal) -->
    <div v-if="!embedded" class="flex items-center justify-between px-6 py-4 border-b border-zinc-200 dark:border-border-dark">
      <h2 class="text-lg font-semibold text-zinc-900 dark:text-zinc-100">Comments</h2>
      <button
        @click="emit('close')"
        class="p-2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors"
      >
        <span class="material-symbols-outlined">close</span>
      </button>
    </div>

    <!-- Comments List -->
    <div class="flex-1 overflow-y-auto p-6 space-y-4">
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <div class="animate-spin w-8 h-8 border-3 border-teal border-t-transparent rounded-full"></div>
      </div>

      <div v-else-if="comments.length === 0" class="text-center py-12">
        <span class="material-symbols-outlined text-4xl text-zinc-300 dark:text-zinc-600 mb-2">chat</span>
        <p class="text-zinc-500 dark:text-zinc-400">No comments yet</p>
        <p class="text-sm text-zinc-400 dark:text-zinc-500">Be the first to comment!</p>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="comment in comments"
          :key="comment.id"
          class="bg-zinc-50 dark:bg-surface-dark rounded-lg p-4"
        >
          <!-- Comment Header -->
          <div class="flex items-start justify-between mb-2">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-full bg-teal/20 flex items-center justify-center text-teal text-sm font-semibold">
                {{ comment.createdByName?.charAt(0) || '?' }}
              </div>
              <div>
                <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100">
                  {{ comment.createdByName || 'Unknown User' }}
                </p>
                <p class="text-xs text-zinc-400">{{ formatDate(comment.createdAt) }}</p>
              </div>
            </div>
            <div v-if="isOwner(comment)" class="flex items-center gap-1">
              <button
                @click="startEdit(comment)"
                class="p-1 text-zinc-400 hover:text-teal rounded transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-lg">edit</span>
              </button>
              <button
                @click="deleteComment(comment.id)"
                class="p-1 text-zinc-400 hover:text-red-500 rounded transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-lg">delete</span>
              </button>
            </div>
          </div>

          <!-- Comment Content or Edit Form -->
          <div v-if="editingComment === comment.id" class="mt-2">
            <textarea
              v-model="editContent"
              class="w-full p-3 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-sm resize-none focus:ring-2 focus:ring-teal/50 focus:border-teal"
              rows="3"
            ></textarea>
            <div class="flex justify-end gap-2 mt-2">
              <button
                @click="cancelEdit"
                class="px-3 py-1.5 text-sm text-zinc-600 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                @click="updateComment(comment.id)"
                class="px-3 py-1.5 text-sm bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
              >
                Save
              </button>
            </div>
          </div>
          <p v-else class="text-sm text-zinc-700 dark:text-zinc-300 whitespace-pre-wrap">
            {{ comment.content }}
          </p>

          <!-- Reply Section -->
          <div class="mt-3 pt-3 border-t border-zinc-200 dark:border-border-dark">
            <button
              v-if="replyingTo !== comment.id"
              @click="replyingTo = comment.id"
              class="text-sm text-teal hover:text-teal/80 font-medium flex items-center gap-1"
            >
              <span class="material-symbols-outlined text-base">reply</span>
              Reply
              <span v-if="comment.replyCount > 0" class="text-zinc-400">({{ comment.replyCount }})</span>
            </button>

            <!-- Reply Form -->
            <div v-if="replyingTo === comment.id" class="mt-3">
              <textarea
                v-model="replyContent"
                placeholder="Write a reply..."
                class="w-full p-3 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-sm resize-none focus:ring-2 focus:ring-teal/50 focus:border-teal"
                rows="2"
              ></textarea>
              <div class="flex justify-end gap-2 mt-2">
                <button
                  @click="replyingTo = null; replyContent = ''"
                  class="px-3 py-1.5 text-sm text-zinc-600 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="addReply(comment.id)"
                  class="px-3 py-1.5 text-sm bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
                >
                  Reply
                </button>
              </div>
            </div>

            <!-- Show Replies -->
            <div v-if="comment.replyCount > 0 && !comment.replies" class="mt-2">
              <button
                @click="loadReplies(comment.id)"
                class="text-sm text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300"
              >
                Show {{ comment.replyCount }} {{ comment.replyCount === 1 ? 'reply' : 'replies' }}
              </button>
            </div>

            <!-- Replies List -->
            <div v-if="comment.replies && comment.replies.length > 0" class="mt-3 pl-4 border-l-2 border-zinc-200 dark:border-border-dark space-y-3">
              <div
                v-for="reply in comment.replies"
                :key="reply.id"
                class="bg-white dark:bg-background-dark rounded-lg p-3"
              >
                <div class="flex items-center gap-2 mb-1">
                  <div class="w-6 h-6 rounded-full bg-zinc-200 dark:bg-border-dark flex items-center justify-center text-zinc-600 dark:text-zinc-400 text-xs font-semibold">
                    {{ reply.createdByName?.charAt(0) || '?' }}
                  </div>
                  <span class="text-sm font-medium text-zinc-700 dark:text-zinc-300">{{ reply.createdByName }}</span>
                  <span class="text-xs text-zinc-400">{{ formatDate(reply.createdAt) }}</span>
                </div>
                <p class="text-sm text-zinc-600 dark:text-zinc-400">{{ reply.content }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- New Comment Form -->
    <div class="p-6 border-t border-zinc-200 dark:border-border-dark">
      <div class="flex gap-3">
        <div class="w-8 h-8 rounded-full bg-teal flex items-center justify-center text-white text-sm font-semibold flex-shrink-0">
          {{ authStore.user?.firstName?.charAt(0) || authStore.user?.username?.charAt(0) || '?' }}
        </div>
        <div class="flex-1">
          <textarea
            v-model="newComment"
            placeholder="Add a comment..."
            class="w-full p-3 border border-zinc-200 dark:border-border-dark rounded-lg bg-zinc-50 dark:bg-surface-dark text-sm resize-none focus:ring-2 focus:ring-teal/50 focus:border-teal focus:bg-white dark:focus:bg-zinc-900"
            rows="3"
            @keydown.ctrl.enter="addComment"
          ></textarea>
          <div class="flex justify-between items-center mt-2">
            <span class="text-xs text-zinc-400">Ctrl + Enter to submit</span>
            <button
              @click="addComment"
              :disabled="!newComment.trim()"
              class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Comment
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
