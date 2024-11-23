// Copyright 2023 Dolphin Emulator Project
// SPDX-License-Identifier: GPL-2.0-or-later

// Partially based on:
// Skyline
// SPDX-License-Identifier: MPL-2.0
// Copyright © 2022 Skyline Team and Contributors (https://github.com/skyline-emu/)

package org.psr.gse;

import android.annotation.SuppressLint;
import android.annotation.TargetApi;
import android.content.res.AssetFileDescriptor;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.graphics.Point;
import android.os.CancellationSignal;
import android.os.ParcelFileDescriptor;
import android.provider.DocumentsContract;
import android.provider.DocumentsProvider;
import android.webkit.MimeTypeMap;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.ArrayList;
import java.util.regex.Pattern;

/** @noinspection ReassignedVariable */

public class DocumentProvider extends DocumentsProvider
{
	public static final String ROOT_ID = "root";

	private static final String[] DEFAULT_ROOT_PROJECTION = new String[]
	{
			DocumentsContract.Root.COLUMN_ROOT_ID,
			DocumentsContract.Root.COLUMN_MIME_TYPES,
			DocumentsContract.Root.COLUMN_FLAGS,
			DocumentsContract.Root.COLUMN_ICON,
			DocumentsContract.Root.COLUMN_TITLE,
			DocumentsContract.Root.COLUMN_SUMMARY,
			DocumentsContract.Root.COLUMN_DOCUMENT_ID,
			DocumentsContract.Root.COLUMN_AVAILABLE_BYTES
	};

	private static final String[] DEFAULT_DOCUMENT_PROJECTION = new String[]
	{
			DocumentsContract.Document.COLUMN_DOCUMENT_ID,
			DocumentsContract.Document.COLUMN_MIME_TYPE,
			DocumentsContract.Document.COLUMN_DISPLAY_NAME,
			DocumentsContract.Document.COLUMN_LAST_MODIFIED,
			DocumentsContract.Document.COLUMN_FLAGS,
			DocumentsContract.Document.COLUMN_SIZE
	};

	private File _rootDirectory = null;

	@Override
	@SuppressWarnings("ConstantConditions")
	public boolean onCreate()
	{
		_rootDirectory = getContext().getExternalFilesDir(null);
		return true;
	}

	@Override
	@SuppressWarnings("ConstantConditions")
	public Cursor queryRoots(String[] projection)
	{
		var result = new MatrixCursor(projection != null ? projection : DEFAULT_ROOT_PROJECTION);

		if (_rootDirectory == null)
		{
			return result;
		}

		var row = result.newRow();
		row.add(DocumentsContract.Root.COLUMN_ROOT_ID, ROOT_ID);
		row.add(DocumentsContract.Root.COLUMN_TITLE, getContext().getString(R.string.app_name));
		row.add(DocumentsContract.Root.COLUMN_ICON, R.mipmap.ic_launcher);
		row.add(DocumentsContract.Root.COLUMN_FLAGS,
				DocumentsContract.Root.FLAG_SUPPORTS_CREATE | DocumentsContract.Root.FLAG_SUPPORTS_RECENTS | DocumentsContract.Root.FLAG_SUPPORTS_SEARCH);
		row.add(DocumentsContract.Root.COLUMN_DOCUMENT_ID, ROOT_ID);

		return result;
	}

	@Override
	public Cursor queryDocument(String documentId, String[] projection) throws FileNotFoundException
	{
		var result = new MatrixCursor(projection != null ? projection : DEFAULT_DOCUMENT_PROJECTION);

		if (_rootDirectory == null)
		{
			return result;
		}

		var file = documentIdToPath(documentId);
		appendDocument(file, result);
		return result;
	}

	@Override
	@SuppressWarnings("ConstantConditions")
	public Cursor queryChildDocuments(String parentDocumentId, String[] projection, String queryArgs) throws FileNotFoundException
	{
		var result = new MatrixCursor(projection != null ? projection : DEFAULT_DOCUMENT_PROJECTION);

		if (_rootDirectory == null)
		{
			_rootDirectory = getContext().getExternalFilesDir(null);
		}

		if (_rootDirectory == null)
		{
			return result;
		}

		var folder = documentIdToPath(parentDocumentId);
		var files = folder.listFiles();
		if (files != null)
		{
			for (var file : files)
			{
				appendDocument(file, result);
			}
		}

		var authority = String.format("%s.user", getContext().getPackageName());
		result.setNotificationUri(getContext().getContentResolver(), DocumentsContract.buildChildDocumentsUri(authority, parentDocumentId));
		return result;
	}

	@Override
	public ParcelFileDescriptor openDocument(String documentId, String mode, CancellationSignal signal) throws FileNotFoundException
	{
		if (_rootDirectory == null)
		{
			return null;
		}

		var file = documentIdToPath(documentId);
		return ParcelFileDescriptor.open(file, ParcelFileDescriptor.parseMode(mode));
	}

	@Override
	public AssetFileDescriptor openDocumentThumbnail(String documentId, Point sizeHint, CancellationSignal signal) throws FileNotFoundException
	{
		var file = documentIdToPath(documentId);
		var pfd = ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
		return new AssetFileDescriptor(pfd, 0, AssetFileDescriptor.UNKNOWN_LENGTH);
	}

	@Override
	@SuppressWarnings("ResultOfMethodCallIgnored")
	public String createDocument(String parentDocumentId, String mimeType, String displayName) throws FileNotFoundException
	{
		if (_rootDirectory == null)
		{
			return null;
		}

		var folder = documentIdToPath(parentDocumentId);
		var file = findFileNameForNewFile(new File(folder, displayName));
		if (mimeType.equals(DocumentsContract.Document.MIME_TYPE_DIR))
		{
			file.mkdirs();
		}
		else
		{
			try
			{
				file.createNewFile();
			}
			catch (IOException e)
			{
				throw new RuntimeException(e);
			}
		}

		refreshDocument(parentDocumentId);
		return pathToDocumentId(file);
	}

	private void deleteChildrenRecursively(File directory) throws IOException
	{
		var children = directory.listFiles();
		if (children == null)
		{
			throw new IOException(String.format("Could not find directory %s", directory.getPath()));
		}

		for (var child : children)
		{
			deleteRecursively(child);
		}
	}

	private void deleteRecursively(File file) throws IOException
	{
		if (file.isDirectory())
		{
			deleteChildrenRecursively(file);
		}

		if (!file.delete())
		{
			throw new IOException(String.format("Failed to delete %s", file.getPath()));
		}
	}

	@Override
	@SuppressWarnings("ConstantConditions")
	public void deleteDocument(String documentId) throws FileNotFoundException
	{
		if (_rootDirectory == null)
		{
			return;
		}

		var file = documentIdToPath(documentId);
		var fileParent = file.getParentFile();
		try
		{
			deleteRecursively(file);
		}
		catch (IOException e)
		{
			throw new RuntimeException(e);
		}

		refreshDocument(pathToDocumentId(fileParent));
	}

	@Override
	@SuppressWarnings({"ConstantConditions", "ResultOfMethodCallIgnored"})
	public String renameDocument(String documentId, String displayName) throws FileNotFoundException
	{
		if (_rootDirectory == null)
		{
			return null;
		}

		var file = documentIdToPath(documentId);
		var dest = findFileNameForNewFile(new File(file.getParentFile(), displayName));
		file.renameTo(dest);
		refreshDocument(pathToDocumentId(file.getParentFile()));
		return pathToDocumentId(dest);
	}

	@SuppressWarnings("ConstantConditions")
	private void refreshDocument(String parentDocumentId)
	{
		var authority = String.format("%s.user", getContext().getPackageName());
		var parentUri = DocumentsContract.buildChildDocumentsUri(authority, parentDocumentId);
		getContext().getContentResolver().notifyChange(parentUri, null);
	}

	@Override
	public boolean isChildDocument(String parentDocumentId, String documentId)
	{
		return documentId.startsWith(parentDocumentId);
	}

	@SuppressWarnings("ConstantConditions")
	private void appendDocument(File file, MatrixCursor cursor)
	{
		var flags = 0;
		if (file.canWrite())
		{
			flags = file.isDirectory()
					? DocumentsContract.Document.FLAG_DIR_SUPPORTS_CREATE
					: DocumentsContract.Document.FLAG_SUPPORTS_WRITE;
			flags |= DocumentsContract.Document.FLAG_SUPPORTS_DELETE | DocumentsContract.Document.FLAG_SUPPORTS_RENAME;
			// The system will handle copy + move for us
		}

		var name = file == _rootDirectory
				? getContext().getString(R.string.app_name)
				: file.getName();

		var mimeType = getTypeForFile(file);
		if (file.exists() && mimeType.startsWith("image/"))
		{
			flags |= DocumentsContract.Document.FLAG_SUPPORTS_THUMBNAIL;
		}

		var row = cursor.newRow();
		row.add(DocumentsContract.Document.COLUMN_DOCUMENT_ID, pathToDocumentId(file));
		row.add(DocumentsContract.Document.COLUMN_MIME_TYPE, getTypeForFile(file));
		row.add(DocumentsContract.Document.COLUMN_DISPLAY_NAME, name);
		row.add(DocumentsContract.Document.COLUMN_LAST_MODIFIED, file.lastModified());
		row.add(DocumentsContract.Document.COLUMN_FLAGS, flags);
		row.add(DocumentsContract.Document.COLUMN_SIZE, file.length());
		if (file == _rootDirectory)
		{
			row.add(DocumentsContract.Document.COLUMN_ICON, R.mipmap.ic_launcher);
		}
	}

	// https://stackoverflow.com/a/40995124
	public static String[] splitPath(String path)
	{
		var ret = new ArrayList<String>();
		var p = Pattern.compile("/+");
		var m = p.matcher(path);
		var s0 = 0;
		int s1, e1;
		while (m.find())
		{
			s1 = m.start();
			e1 = m.end();
			if (s1 - s0 > 0)
			{
				ret.add(path.substring(s0, s1));
			}

			s0 = e1;
		}

		if (s0 < path.length())
		{
			ret.add(path.substring(s0));
		}

		return ret.toArray(new String[0]);
	}

	public static String relativize(String baseDir, String path)
	{
		if (!baseDir.endsWith("/"))
		{
			baseDir = baseDir + "/"; // assume the baseDir is always a directory.
		}

		var bases = splitPath(baseDir);
		var paths = splitPath(path);
		int p = 0, q = 0;

		while (p < bases.length && q < paths.length && bases[p].equals(paths[q]))
		{
			p++;
			q++;
		}

		var sb = new StringBuilder(255);
		for (var i = bases.length - 1; i >= p; i--)
		{
			sb.append("../");
		}

		for (var i = q; i < paths.length; i++)
		{
			sb.append(paths[i]);
			if (i != paths.length - 1)
			{
				sb.append("/");
			}
		}

		if (path.endsWith("/"))
		{
			// ensure the last char of sb is not /
			var i = sb.length();
			if (i > 0 && sb.charAt(i - 1) != '/')
			{
				sb.append("/");
			}
		}

		return sb.toString();
	}

	private String pathToDocumentId(File path)
	{
		var basePath = _rootDirectory.getAbsolutePath();
		var targetPath = path.getAbsolutePath();
		return String.format("%s/%s", ROOT_ID, relativize(basePath, targetPath));
	}

	private File documentIdToPath(String documentId) throws FileNotFoundException
	{
		var file = new File(_rootDirectory, documentId.substring(ROOT_ID.length()));
		if (!file.exists())
		{
			throw new FileNotFoundException(String.format("File %s does not exist.", documentId));
		}

		return file;
	}

	private String getTypeForFile(File file)
	{
		if (file.isDirectory())
		{
			return DocumentsContract.Document.MIME_TYPE_DIR;
		}
		else
		{
			var fileName = file.getName();
			var extDotIndex = fileName.lastIndexOf('.');
			if (extDotIndex == -1)
			{
				return "application/octet-stream";
			}

			var extension = fileName.substring(extDotIndex + 1);
			var mimeType = MimeTypeMap.getSingleton().getMimeTypeFromExtension(extension);
			if (mimeType == null)
			{
				return "application/octet-stream";
			}

			return mimeType;
		}
	}

	@SuppressLint("DefaultLocale")
	private File findFileNameForNewFile(File file)
	{
		var i = 1;
		while (file.exists())
		{
			var path = file.getAbsolutePath();
			var extDotIndex = path.lastIndexOf('.');
			if (extDotIndex == -1)
			{
				file = new File(String.format("%s.%d", path, i));
			}
			else
			{
				var pathWithoutExtension = path.substring(0, extDotIndex);
				var extension = path.substring(extDotIndex + 1);
				file = new File(String.format("%s.%d.%s", pathWithoutExtension, i, extension));
			}

			i++;
		}

		return file;
	}
}
